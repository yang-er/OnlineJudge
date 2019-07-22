using Microsoft.Extensions.Primitives;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.FileProviders
{
    public class AreasFileProvider : IFileProvider, IDirectoryContents
    {
        const string Areas = "/Areas/";

        public string Real { get; }

        private PhysicalFileProvider SolutionFileProvider { get; }

        private List<string> AreaNames { get; }

        bool IDirectoryContents.Exists => true;

        public AreasFileProvider(PhysicalFileProvider solution, string real)
        {
            SolutionFileProvider = solution;
            AreaNames = new List<string>();
            Real = "/" + real;
        }

        public void AddExternalArea(string areaName)
        {
            AreaNames.Add(areaName);
        }

        public void AddExternalAreas(IEnumerable<string> vs)
        {
            AreaNames.AddRange(vs);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            foreach (var areaName in AreaNames)
                if (subpath.StartsWith(Areas + areaName))
                    return SolutionFileProvider.GetFileInfo(
                        Real + subpath.Substring(Areas.Length));

            return new NotFoundFileInfo(subpath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (subpath == "/Areas") return this;

            foreach (var areaName in AreaNames)
                if (subpath.StartsWith(Areas + areaName))
                    return SolutionFileProvider.GetDirectoryContents(
                        Real + subpath.Substring(Areas.Length));

            return NotFoundDirectoryContents.Singleton;
        }

        public IChangeToken Watch(string filter)
        {
            foreach (var areaName in AreaNames)
                if (filter.StartsWith(Areas + areaName))
                    return SolutionFileProvider.Watch(
                        Real + filter.Substring(Areas.Length));

            return NullChangeToken.Singleton;
        }

        private IEnumerable<IFileInfo> GetAreasFileInfo()
        {
            foreach (var item in AreaNames)
            {
                var dr = new DirectoryInfo(SolutionFileProvider.Root + Real + item);
                var dd = new Physical.PhysicalDirectoryInfo(dr);
                yield return new AreaDirectoryInfo(dd, item);
            }
        }

        IEnumerator<IFileInfo> IEnumerable<IFileInfo>.GetEnumerator()
        {
            return GetAreasFileInfo().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetAreasFileInfo().GetEnumerator();
        }
    }
}
