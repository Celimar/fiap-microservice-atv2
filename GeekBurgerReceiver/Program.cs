using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GeekBurgerReceiver
{
    class Program
    {
        const string QueueConnectionString = "Endpoint=sb://geekburgerproductscbo.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=aq/72EU3dXF1z9vi+zYDROrlBM/+YjerCSheVyBEbZI=";
        const string QueuePath = "productchanged";
        static IQueueClient _queueClient;
        private static int count = 0;

        public static List<Task> PendingCompleteTasks { get; private set; }

        private static void Main()
        {
            count = 0;
            PendingCompleteTasks = new List<Task>();

            ReceiveMessagesAsync().GetAwaiter().GetResult();
            Console.WriteLine("messages were sent");
            Console.ReadLine();
        }

        //private static async Task ReceiveMessagesAsync()
        //{
        //    _queueClient = new QueueClient(QueueConnectionString, QueuePath);
        //    _queueClient.RegisterMessageHandler(MessageHandler, new MessageHandlerOptions(ExceptionHandler) { AutoComplete = false });
        //    Console.ReadLine();
        //    await _queueClient.CloseAsync();
        //}
        private static async Task ReceiveMessagesAsync()
        {
            _queueClient = new QueueClient(QueueConnectionString, QueuePath, ReceiveMode.PeekLock);
            _queueClient.RegisterMessageHandler(MessageHandler, new MessageHandlerOptions(ExceptionHandler) { AutoComplete = false });
            Console.ReadLine();
            Console.WriteLine($" Request to close async. Pending tasks: { PendingCompleteTasks.Count} ");
            await Task.WhenAll(PendingCompleteTasks);
            Console.WriteLine($"All pending tasks were completed");
            var closeTask = _queueClient.CloseAsync();
            await closeTask;
            CheckCommunicationExceptions(closeTask);
        }


        private static void CheckCommunicationExceptions(Task closeTask)
        {
            throw new NotImplementedException();
        }

        private static Task ExceptionHandler(ExceptionReceivedEventArgs exceptionArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionArgs.Exception}.");
            var context = exceptionArgs.ExceptionReceivedContext;
            Console.WriteLine($"Endpoint:{context.Endpoint}, Path:{context.EntityPath}, Action:{context.Action}");
            return Task.CompletedTask;
        }

        //private static async Task MessageHandler(Message message, CancellationToken cancellationToken)
        //{
        //    Console.WriteLine($"Received message:{ Encoding.UTF8.GetString(message.Body)}");
        //    await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
        //}

        private static async Task MessageHandler(Message message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Received message{ Encoding.UTF8.GetString(message.Body)} ");

            if (cancellationToken.IsCancellationRequested || _queueClient.IsClosedOrClosing)
                return;

            Console.WriteLine($"task {count++}");
            Task PendingCompleteTask;
            lock (PendingCompleteTasks)
            {
                PendingCompleteTasks.Add(_queueClient.CompleteAsync( message.SystemProperties.LockToken));
                PendingCompleteTask = PendingCompleteTasks.LastOrDefault();
            }
            Console.WriteLine($"calling complete for task {count}");
            await PendingCompleteTask;

            Console.WriteLine($"remove task {count} from task queue");
            PendingCompleteTasks.Remove(PendingCompleteTask);
        }
    }
}
