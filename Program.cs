using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Type;
using EvernoteSDK;
using EvernoteSDK.Advanced;

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

            ENSessionAdvanced.SetSharedSessionDeveloperToken(
                developerToken,
                notestoreUrl);

            var session = ENSessionAdvanced.SharedSession;
            var notebooks = session.PrimaryNoteStore.ListLinkedNotebooks();

            LinkedNotebook recipes = notebooks.First(notebook => notebook.ShareName == "recipes");
            _recipesStore = session.NoteStoreForLinkedNotebook(recipes);

            var recipesByTag = new Dictionary<string, IList<string>>();
            foreach (var note in FindNotes(new NoteFilter()))
            {
                //Console.WriteLine("{0}", note.Title);
                foreach (string tagName in GetTagNames(note))
                {
                    IList<string> recipeNames;
                    if (recipesByTag.TryGetValue(tagName, out recipeNames))
                    {
                        recipeNames.Add(note.Title);
                    }
                    else
                    {
                        recipeNames = new List<string> { note.Title };
                        recipesByTag[tagName] = recipeNames;
                    }
                }
            }

            var tagNames = recipesByTag.Keys.OrderBy(k => k);
            foreach (string tagName in tagNames)
            {
                Console.WriteLine();
                Console.WriteLine(tagName);
                foreach (string recipeName in recipesByTag[tagName])
                {
                    Console.WriteLine("\t{0}", recipeName);
                }
            }
        }

        private static readonly List<string> EmptyTagGuids = new List<string>();
        private static readonly Dictionary<Guid, string> TagNameByGuid = new Dictionary<Guid, string>();
        private static ENNoteStoreClient _recipesStore;

        private static IEnumerable<string> GetTagNames(Note note)
        {
            foreach (string guid in note.TagGuids ?? EmptyTagGuids )
            {
                var tagGuid = Guid.Parse(guid);
                string tagName;
                if (!TagNameByGuid.TryGetValue(tagGuid, out tagName))
                {
                    Tag tag = _recipesStore.GetTag(guid);
                    tagName = tag.Name;
                    TagNameByGuid[tagGuid] = tagName;
                }

                yield return tagName;
            }
        }

        private static IEnumerable<Note> FindNotes(NoteFilter noteFilter)
        {
            // TODO: make several paged calls
            NoteList noteList = _recipesStore.FindNotes(noteFilter, 0, 1000);
            List<Note> notes = noteList.Notes;
            return notes;
        }
    }
}
