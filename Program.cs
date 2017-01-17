using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EvernoteSDK;

namespace EvernoteRecipesByTag
{
    class Program
    {
        static void Main(string[] args)
        {
            string developerToken;
            if (args.Length > 0)
            {
                developerToken = args[0];
            }
            else
            {
                Console.WriteLine("Enter developer token:");
                developerToken = Console.ReadLine();
            }

            string notestoreUrl;
            if (args.Length > 1)
            {
                notestoreUrl = args[1];
            }
            else
            {
                Console.WriteLine("Enter note store URL:");
                notestoreUrl = Console.ReadLine();
            }

            ENSession.SetSharedSessionDeveloperToken(
                developerToken,
                notestoreUrl);

            var session = ENSession.SharedSession;
            foreach (var notebook in session.ListNotebooks())
            {
                Console.WriteLine(notebook.Name);
            }
        }
    }
}
