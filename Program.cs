// The code is under MIT-0 license <https://github.com/aws/mit-0>
using System;
using System.Threading;

using Nancy;
using Nancy.Hosting.Self;
using RandomNameGeneratorLibrary;
using Newtonsoft.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;


namespace SampleServer
{

  public class VersionModule : NancyModule {
    public VersionModule() {
      Get("/", parameters => "Version 0.2");
    }
  }

  class Program {

        const string unique = "";
        const string QUEUE_SSM_PARAMETER_NAME= "/monolith/policyQueueUrl" + unique;
        static PersonNameGenerator personGenerator = new PersonNameGenerator();

        static string queueUrl;
        static AmazonSQSClient queue;
    static Random random = new Random();
    static async Task Main(string[] args) {

            var ssmClient = new AmazonSimpleSystemsManagementClient();
            var queueUrlParam = await ssmClient.GetParameterAsync(new GetParameterRequest { Name = QUEUE_SSM_PARAMETER_NAME });
            queueUrl = queueUrlParam.Parameter.Value;
            queue = new AmazonSQSClient();


      using (var nancyHost = new NancyHost(new Uri("http://localhost:8888/"))) {
        nancyHost.Start();

        Console.WriteLine("Nancy now listening - navigating to http://localhost:8888/. Press enter to stop");


                var timer = new Timer(async (x) => { await SavePolicy(x); }, null, 0, 3000);
                Console.ReadKey();
                timer.Dispose();
            }

      Console.WriteLine("Stopped. Good bye!");
    }

    private static async Task SavePolicy(object state) {
      var policy = new Policy {
        PolicyOwner = personGenerator.GenerateRandomFirstName
        (),
        CprNo = GenerateCprNo()
      };
            //PersistPolicy(policy);
            await SendMessage(policy);
    }

        private static async Task SendMessage(Policy policy)
        {
            
            var  body =JsonConvert.SerializeObject (policy);

            var resp = await queue.SendMessageAsync(new SendMessageRequest { QueueUrl=queueUrl,
            MessageGroupId="policies",
            MessageDeduplicationId=policy.CprNo,
            MessageBody=body
            
            });

            Console.WriteLine($"Sent {body},status {resp.HttpStatusCode}");
        }

        private static string GenerateCprNo() {
      var daysOld = random.Next(20 * 365, 100 * 365);
      var bday = DateTime.Today.AddDays(-daysOld);
      var seq = random.Next(1000, 9999);
      return bday.ToString("ddMMyy") + "-" + seq.ToString();
    }

    private static void PersistPolicy(Policy policy) {
      Console.WriteLine($"CPR: {policy.CprNo} Owner: {policy.PolicyOwner}");
    }
  }

  public class Policy {
    public string PolicyOwner { get; set; }
    public string CprNo { get; set; }
  }
}
