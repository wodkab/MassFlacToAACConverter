using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AACMassEncoder
{

    public class SyncDirectories
    {
        private readonly string SourcePath;
        private readonly string ToSyncPath;
        private IList<FileItem> SourceFiles;

        public SyncDirectories(string sourcePath, string toSyncPath, IList<FileItem>sourceFiles)
        {
            SourcePath = sourcePath;
            ToSyncPath = toSyncPath;
            SourceFiles = sourceFiles;
        }

        private IEnumerable<string> ToSyncFiles => Directory.GetFiles(ToSyncPath, "*", SearchOption.AllDirectories);

        public void Sync()
        {
            var syncFileItems = ToSyncFiles.Select(file =>
                new FileItem(new FileInfo(file), SourcePath, ToSyncPath, String.Empty)).ToList();

            foreach (var syncFile in syncFileItems)
            {
                //No original on source site => delete
                //if(syncFile)
            }
        }
    }
}
