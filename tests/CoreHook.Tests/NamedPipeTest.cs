﻿using CoreHook.IPC.NamedPipes;
using CoreHook.IPC.Platform;
using System;
using Xunit;

namespace CoreHook.Tests
{
    public class NamedPipeTest
    {
        [Fact]
        private void ShouldConnectToServer()
        {
            string namedPipe = Resources.GetUniquePipeName();
            const string testMessage = "TestMessage";
            bool receivedMessage = false;

            using (CreateServer(namedPipe, new PipePlatformBase(),
                (string request, IPC.IConnection connection) =>
                {
                    receivedMessage = true;
                    connection.TrySendResponse("RandomResponse");
                }))
            {
                using (INamedPipeClient pipeClient = new NamedPipeClient(namedPipe))
                {
                    if(SendPipeMessage(pipeClient, testMessage))
                    {
                        pipeClient.ReadRawResponse();
                    }
                }
            }
            Assert.True(receivedMessage);
        }

        [Fact]
        private void ShouldConnectToServerAndReceiveResponse()
        {
            string namedPipe = Resources.GetUniquePipeName();
            const string testMessage = "TestMessage";
            bool receivedCorrectMessage = false;
            
            using (var pipeServer = CreateServer(namedPipe, new PipePlatformBase(),
                (string request, IPC.IConnection connection) =>
                {
                    if (request == testMessage)
                    {
                        receivedCorrectMessage = true;
                    }
                    connection.TrySendResponse(request);
                }))
            {
                using (INamedPipeClient pipeClient = new NamedPipeClient(namedPipe))
                {
                    if (SendPipeMessage(pipeClient, testMessage))
                    {
                        Assert.Equal(pipeClient.ReadRawResponse(), testMessage);
                    }
                }
            }
            Assert.True(receivedCorrectMessage);
        }

        [Fact]
        private void ShouldConnectToServerAndReceiveMultipleResponses()
        {
            string namedPipe = Resources.GetUniquePipeName();
            const string testMessage1 = "TestMessage1";
            const string testMessage2 = "TestMessage2";
            const string testMessage3 = "TestMessage3";

            using (CreateServer(namedPipe, new PipePlatformBase(),
                (string request, IPC.IConnection connection) =>
                {
                    connection.TrySendResponse(request);
                }))
            {
                using (INamedPipeClient pipeClient = new NamedPipeClient(namedPipe))
                {
                    if (pipeClient.Connect(3000))
                    {
                        pipeClient.SendRequest(testMessage1);
                        pipeClient.SendRequest(testMessage2);
                        pipeClient.SendRequest(testMessage3);

                        Assert.Equal(pipeClient.ReadRawResponse(), testMessage1);
                        Assert.Equal(pipeClient.ReadRawResponse(), testMessage2);
                        Assert.Equal(pipeClient.ReadRawResponse(), testMessage3);
                    }
                }
            }
        }

        [Fact]
        private void ShouldConnectToServerAndReceiveRandomResponse()
        {
            string namedPipe = Resources.GetUniquePipeName();
            const string testMessage = "TestMessage";
            bool receivedCorrectMessage = false;

            using (CreateServer(namedPipe, new PipePlatformBase(),
                (string request, IPC.IConnection connection) =>
                {
                    if (request == testMessage)
                    {
                        receivedCorrectMessage = true;
                    }
                    connection.TrySendResponse("RandomResponse");
                }))
            {
                using (INamedPipeClient pipeClient = new NamedPipeClient(namedPipe))
                {
                    if (SendPipeMessage(pipeClient, testMessage))
                    {
                        Assert.NotEqual(pipeClient.ReadRawResponse(), testMessage);
                    }
                }
            }
            Assert.True(receivedCorrectMessage);
        }

        [Fact]
        private void ShouldNotConnectToServer()
        {
            string clientNamedPipe = Resources.GetUniquePipeName();
            bool connected = false;
   
            using (INamedPipeClient pipeClient = new NamedPipeClient(clientNamedPipe))
            {
                if(pipeClient.Connect(1000))
                {
                    connected = true;
                }
            }
            
            Assert.False(connected);
        }

        private static INamedPipeServer CreateServer(string namedPipeName, IPipePlatform pipePlatform, Action<string, IPC.IConnection> handleRequest)
        {
            return NamedPipeServer.StartNewServer(namedPipeName, pipePlatform, handleRequest);
        }
        
        private static bool SendPipeMessage(INamedPipeClient pipeClient, string message)
        {
            if (pipeClient.Connect(3000))
            {
                pipeClient.SendRequest(message);
                return true;
            }
            return false;
        }
    }
}
