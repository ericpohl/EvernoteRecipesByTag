using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Type;
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

            string outputPath;
            if (args.Length > 2)
            {
                outputPath = args[2];
            }
            else
            {
                Console.WriteLine("Enter output path:");
                outputPath = Console.ReadLine();
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
                foreach (string tagName in GetTagNames(note))
                {
                    IList<string> recipeNames;
                    if (recipesByTag.TryGetValue(tagName, out recipeNames))
                    {
                        string title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(note.Title);
                        recipeNames.Add(title);
                    }
                    else
                    {
                        recipeNames = new List<string> { note.Title };
                        recipesByTag[tagName] = recipeNames;
                    }
                }
            }

            using (var writer = new StreamWriter(outputPath))
            {
                WriteRecipesByTag(recipesByTag, writer);
            }
        }

        private static void WriteRecipesByTag(Dictionary<string, IList<string>> recipesByTag, TextWriter textWriter)
        {
            var tagNames = recipesByTag.Keys.OrderBy(k => k);
            foreach (string tagName in tagNames)
            {
                textWriter.WriteLine();
                textWriter.WriteLine(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tagName));
                foreach (string recipeName in recipesByTag[tagName].OrderBy(TrimLeadingArticle))
                {
                    textWriter.WriteLine("\t{0}", recipeName);
                }
            }
        }

        private static string TrimLeadingArticle(string s)
        {
            var articles = new string[] {"the", "a", "an"};
            foreach (string article in articles)
            {
                if (s.IndexOf(article + " ", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    return s.Substring(article.Length + 1).Trim();
                }
            }

            return s;
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
            int offset = 0;
            int pageSize = 50;
            int totalSize = -1;

            do
            {
                if (totalSize > -1)
                {
                    pageSize = Math.Min(pageSize, totalSize - offset);
                }
                NoteList noteList = _recipesStore.FindNotes(noteFilter, offset, pageSize);
                totalSize = noteList.TotalNotes;
                offset += noteList.Notes.Count;
                foreach (var note in noteList.Notes)
                {
                    yield return note;
                }
            } while (offset < totalSize);
        }
    }
}
