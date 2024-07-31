using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using Newtonsoft.Json;
using RemoteAdmin;

namespace CocoaPlugin.API.Managers
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
                        if (context.Request.HttpMethod != "POST")
                        {
                            context.Response.StatusCode = 405;
                            context.Response.OutputStream.Close();
                            return;
                        }

                        if (context.Request.InputStream == null)
                        {
                            context.Response.StatusCode = 400;
                            context.Response.OutputStream.Close();
                            return;
                        }

                        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                        var requestBody = await reader.ReadToEndAsync();

                        var command = JsonConvert.DeserializeObject<DiscordCommand>(requestBody);

                        if (command == null)
                        {
                            context.Response.StatusCode = 400;
                            context.Response.OutputStream.Close();
                            return;
                        }

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
                case "/serverinfo":
                    try
                    {
                        var data = new
                        {
                            PlayerCount = Player.List.Count,
                            MaxPlayerCount = Server.MaxPlayerCount,
                        };

                        var responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength64 = responseBytes.Length;
                        await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error while processing ServerInfo Request:\n{ex}");
                    }
                    finally
                    {
                        context.Response.OutputStream.Close();
                    }

                    break;
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

        public static bool CanSend = true;

        public static void SendDm(object content)
        {
            if (!CanSend)
            {
                Log.Warn("NetworkManager is disabled.");
                return;
            }

            _ = SendAsync(content, PostType.LinkDm);
        }

        public static void SendLog(object content, LogType type)
        {
            if (!CanSend)
            {
                Log.Warn("NetworkManager is disabled.");
                return;
            }

            _ = SendAsync(new { Content = content, Type = type }, PostType.Log);
        }

        private static async Task SendAsync(object message, PostType type)
        {
            var url = CocoaPlugin.Instance.Config.Network.PostBaseUrl + CocoaPlugin.Instance.Config.Network.SubUrl[type];

            try
            {
                var request = WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";

                var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                await using var stream = await request.GetRequestStreamAsync();
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);

                using var response = await request.GetResponseAsync();
                using var reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException(), Encoding.UTF8);
                var responseBody = await reader.ReadToEndAsync();

                Log.Debug($"Response from {url}:\n{responseBody}");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("ConnectFailure"))
                {
                    Log.Warn("Cannot connect to the server. Is the bot server running?");
                }
                else
                {
                    Log.Error($"Error while sending message to {url}:\n{ex.Message}");
                }
            }
        }

        #endregion
    }
}
