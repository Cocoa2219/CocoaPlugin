using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Exiled.API.Features;
using Newtonsoft.Json;
using RemoteAdmin;

namespace CocoaPlugin.API
{
    public static class NetworkManager
    {
        #region Network Listener

        private static HttpListener _listener;
        private static CancellationTokenSource _cancellationTokenSource;

        public static async void StartListener()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(CocoaPlugin.Instance.Config.Network.ListenUrl);
                _listener.Start();

                _cancellationTokenSource = new CancellationTokenSource();

                var listenerTask = ListenAsync(_listener, _cancellationTokenSource.Token);

                await listenerTask;
            }
            catch (Exception e)
            {
                Log.Error($"Error while opening {CocoaPlugin.Instance.Config.Network.ListenUrl}:\n{e}");
            }
        }

        private static async Task ListenAsync(HttpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await listener.GetContextAsync();
                    _ = ProcessRequest(context);
                }
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995) // The I/O operation has been aborted because of either a thread exit or an application request.
            {
                Log.Info("Listener has been stopped.");
            }
            catch (Exception ex)
            {
                Log.Error($"Error while processing Request:\n{ex.Message}");
            }
            finally
            {
                listener.Close();
            }
        }

        private static async Task ProcessRequest(HttpListenerContext context)
        {
            switch (context.Request.RawUrl)
            {
                case "/":
                    try
                    {
                        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                        var requestBody = await reader.ReadToEndAsync();

                        var command = JsonConvert.DeserializeObject<DiscordCommand>(requestBody);

                        var response = CommandProcessor.ProcessQuery(command.Command, new DiscordCommandSender(command.Username + "@discord", command.Username, ulong.MaxValue, byte.MaxValue, true));

                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        context.Response.ContentType = "text/plain";
                        context.Response.ContentLength64 = responseBytes.Length;
                        await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error while processing DiscordCommand Request:\n{ex}");
                    }
                    finally
                    {
                        context.Response.OutputStream.Close();
                    }

                    break;
                // case "/ban":
                //     try
                //     {
                //         using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                //         var requestBody = await reader.ReadToEndAsync();
                //
                //         var command = JsonConvert.DeserializeObject<BanCommand>(requestBody);
                //
                //         var player = Player.Get(command.UserId);
                //
                //         var idSuccess = BanHandler.IssueBan(new BanDetails
                //         {
                //             OriginalName = command.UserId,
                //             Id = command.UserId,
                //             IssuanceTime = command.From,
                //             Expires = command.Until,
                //             Reason = command.Reason,
                //             Issuer = command.IssuerId
                //         }, BanHandler.BanType.UserId);
                //
                //         var ipSuccess = BanHandler.IssueBan(new BanDetails
                //         {
                //             OriginalName = command.UserId,
                //             Id = command.IpAddress,
                //             IssuanceTime = command.From,
                //             Expires = command.Until,
                //             Reason = command.Reason,
                //             Issuer = command.IssuerId
                //         }, BanHandler.BanType.IP);
                //
                //         if (player != null)
                //             ServerConsole.Disconnect(player.ReferenceHub.gameObject, "You have been banned. Reason: " + command.Reason);
                //
                //         var success = idSuccess && ipSuccess;
                //
                //         var response = success ? "Successfully banned player." : "Failed to ban player.";
                //
                //         var responseBytes = Encoding.UTF8.GetBytes(response);
                //         context.Response.ContentType = "text/plain";
                //         context.Response.ContentLength64 = responseBytes.Length;
                //         await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                //     }
                //     catch (Exception ex)
                //     {
                //         Log.Error($"Error while processing DiscordCommand Request:\n{ex}");
                //     }
                //     finally
                //     {
                //         context.Response.OutputStream.Close();
                //     }
                //
                //     break;
                default:
                    context.Response.StatusCode = 404;
                    context.Response.OutputStream.Close();
                    break;
            }
        }

        public static void StopListener()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _listener.Stop();
                _listener.Close();
            }
            catch (Exception ex)
            {
                Log.Error($"Error while stopping the listener:\n{ex}");
            }
        }

        #endregion

        #region Network Sender

        public static void Send(object content, MessageType type)
        {
            _ = SendAsync(new Message { Content = content, Type = type });
        }

        private static async Task SendAsync(Message message)
        {
            try
            {
                var request = WebRequest.Create(CocoaPlugin.Instance.Config.Network.PostUrl);
                request.Method = "POST";
                request.ContentType = "application/json";

                var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                await using var stream = await request.GetRequestStreamAsync();
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);

                using var response = await request.GetResponseAsync();
                using var reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException(), Encoding.UTF8);
                var responseBody = await reader.ReadToEndAsync();

                Log.Debug($"Response from {CocoaPlugin.Instance.Config.Network.PostUrl}:\n{responseBody}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error while sending message to {CocoaPlugin.Instance.Config.Network.PostUrl}:\n{ex.Message}");
            }
        }

        #endregion
    }
}
