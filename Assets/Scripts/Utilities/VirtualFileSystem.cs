/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using ICSharpCode.SharpZipLib.Zip;

namespace VirtualFileSystem
{
    public class VfsEntry : IEnumerable<VfsEntry>
    {
        // we use a lazy loaded implementation so that we only check if it is
        // a archive file we can descend into if someone asks us if it is.
        protected System.Lazy<IVfsImpl> _impl;
        public VfsEntry(string name, bool isFile, long size, VfsEntry parent)
        {
            _name = name;
            _size = size;
            _isFile = isFile;
            _parent = parent;
            _impl = new Lazy<IVfsImpl>(() => CreateImplementation(this));
        }  
        
        public VfsEntry(string name, bool isFile, long size, VfsEntry parent, IVfsImpl implementation)
        {
            _name = name;
            _size = size;
            _isFile = isFile;
            _parent = parent;
            _impl = new Lazy<IVfsImpl>(() => implementation);
        }  

        public static VfsEntry makeRoot(string path)
        {
            return new VfsEntry("", false, 0, null, new VfsPhysicalDirectory(new DirectoryInfo(path)));
        }
        public bool IsFile => _isFile;
        public bool CanDescend  => _impl.Value.CanDescend;
        public long Size => _size;
        public Stream GetStream() => _parent.GetStreamForChild(this);
        private Stream GetStreamForChild(VfsEntry child) => _impl.Value.GetStreamForChild(child);
        public Stream SeekableStream() => VirtualFileSystem.EnsureSeekable(GetStream(), Size);

        public VfsEntry Parent => _parent;
        public IEnumerator<VfsEntry> GetEnumerator() => _impl.Value.GetEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => _impl.Value.GetEnumerator(this);
        
        public string Name => _name;
        protected string _name;
        protected long _size;
        protected bool _isFile;
        protected VfsEntry _parent;
        public string Path => _parent != null ? System.IO.Path.Combine(_parent.Path, _name) : _name; 
        public VfsEntry GetChild(string name) => _impl.Value.GetChild(name, this);
        public VfsEntry Find(string name)
        {
            var parts = name.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            return Find(parts);
        }
        public VfsEntry Find(string[] path)
        {
            // nothing to find
            if (path.Length == 0) return null;

            // find child corresponding to first part of path
            VfsEntry next = _impl.Value.GetChild(path[0], this);
            // its all there is to find - return it
            if(path.Length == 1) return next;

            // we have a longer path to descend into, try to descend on the child
            if(next != null && next.CanDescend)
            {
                 return next.Find(path.Skip(1).ToArray());
            }
            return null;
        }
        
        static IVfsImpl CreateImplementation(VfsEntry entry)
        {
            if(entry.IsFile)
            {
                //22 appears to be the smallest possible zip file
                // TODO potentially we could check for the magic number - would it be faster?
                if(entry.Size > 22)
                {
                    try
                    {
                        return new VfsZipDirectory(entry);
                    }
                    catch
                    {
                        // I found no better way to test if something is a zip file than actually trying it.
                        // this is not a failure so we don't want to log the exception
                    }
                }
                return new VfsPhysicalPlainFile();
            }
            return new VfsPhysicalDirectory(entry);
        }
    }

    public interface IVfsImpl
    {
        bool CanDescend { get; }
        IEnumerator<VfsEntry> GetEnumerator(VfsEntry entry);
        VfsEntry GetChild(string name, VfsEntry entry);
        Stream GetStreamForChild(VfsEntry child);
    }
    
    class VfsPhysicalDirectory : IVfsImpl
    {
        DirectoryInfo _dirInfo;
        public VfsPhysicalDirectory(VfsEntry entry)
        {
            _dirInfo = new DirectoryInfo(entry.Path);
        }
        public VfsPhysicalDirectory(DirectoryInfo info)
        {
            _dirInfo = info;
        }
        public bool CanDescend => true;

        public VfsEntry GetChild(string name, VfsEntry entry)
        {
            string realPath = System.IO.Path.Combine(_dirInfo.FullName, name);
            var dir = new DirectoryInfo(realPath);
            if (dir.Exists)
            {
                return new VfsEntry(dir.Name, false, 0, entry, new VfsPhysicalDirectory(dir));
            }
            var file = new FileInfo(realPath);
            if(file.Exists)
            {
                return new VfsEntry(file.Name, true, file.Length, entry);
            }
            return null;
        }

        public IEnumerator<VfsEntry> GetEnumerator(VfsEntry entry)
        {
            var files = _dirInfo.EnumerateFiles().Select(fileInfo => new VfsEntry(fileInfo.Name, true, fileInfo.Length, entry));
            var dirs = _dirInfo.EnumerateDirectories().Select(dirInfo => new VfsEntry(dirInfo.Name, false, 0, entry, new VfsPhysicalDirectory(dirInfo)));
            return dirs.Concat(files).GetEnumerator();
        }

        public Stream GetStreamForChild(VfsEntry child)
        {
            if(File.Exists(Path.Combine(_dirInfo.FullName, child.Name))) {
                return File.OpenRead(Path.Combine(_dirInfo.FullName, child.Name));
            }
            return null;
        }
    }
    
    class VfsPhysicalPlainFile : IVfsImpl
    {
        public VfsPhysicalPlainFile() { }
        public bool CanDescend => false;
        public VfsEntry GetChild(string name, VfsEntry entry) => null;

        public IEnumerator<VfsEntry> GetEnumerator(VfsEntry entry) 
        {
            yield break;
        }

        public Stream GetStreamForChild(VfsEntry child) => null;
    }
    
    class VfsZipDirectory : IVfsImpl
    {
        ZipFile _zipFile;
        string _pathInZip;
        public VfsZipDirectory(ZipFile zipFile, string pathInZip) {
            _zipFile = zipFile;
            _pathInZip = pathInZip;
        }
        public VfsZipDirectory(VfsEntry entry)
        {
            Stream stream = VirtualFileSystem.EnsureSeekable(entry.GetStream(), entry.Size);
            _zipFile = new ZipFile(stream);
            _pathInZip = "";
        }
        public bool CanDescend => true;
        
        public VfsEntry GetChild(string name, VfsEntry entry)
        {
            string realPath = System.IO.Path.Combine(_pathInZip, name);
            ZipEntry ze = _zipFile.GetEntry(realPath+"/") ?? _zipFile.GetEntry(realPath);

            if(ze == null) return null;
            if(ze.IsFile)
            {
                return new VfsEntry(name, true, ze.Size, entry);
            }
            else
            {
                return new VfsEntry(name, false, 0, entry, new VfsZipDirectory(_zipFile, Path.Combine(_pathInZip, name)));
            }
        }

        public IEnumerator<VfsEntry> GetEnumerator(VfsEntry entry) 
        {
            // adapter on top of ZipFile enumerator to filter only current directory
            return _zipFile.Cast<ZipEntry>()
            .Where((ze) => {
                    // if it does not start with the current subdir, skip it
                    if(!ze.Name.StartsWith(_pathInZip)) return false;
                    // if its name starts with the path and is the same length, it is the same string
                    // but we only want children, not the directory itself.
                    if(ze.Name.Length == _pathInZip.Length) return false;
                    var firstSlash = ze.Name.IndexOf(System.IO.Path.DirectorySeparatorChar, _pathInZip.Length);
                    // if we don't find any subsequent slashes, it has to be a file in the current subdir
                    if(firstSlash == -1) return true;
                    // if we find a slash at the end of the entrys name it is a subdirectory in the current directory
                    if(firstSlash == ze.Name.Length - 1) return true;
                    // otherwise it is something further down in the tree which we don't want here
                    return false;
                } )
            
            .Select((e) => {
                if(e.IsDirectory)
                {
                     return new VfsEntry(Path.GetFileName(e.Name.Remove(e.Name.Length - 1)), false, 0, entry, new VfsZipDirectory(_zipFile, e.Name));
                }
                else
                {
                    return new VfsEntry(Path.GetFileName(e.Name), e.IsFile, e.Size, entry);
                }
                }).GetEnumerator();
        }

        public Stream GetStreamForChild(VfsEntry child) 
        {
            var ze = _zipFile.GetEntry(Path.Combine(_pathInZip, child.Name));
            return _zipFile.GetInputStream(ze);
        }
    }

    class VirtualFileSystem
    {
        static public Stream EnsureSeekable(Stream stream, long length)
        {
            if (!stream.CanSeek)
            {
                var buffer = new byte[length];
                stream.Read(buffer, 0, (int)length);
                stream = new MemoryStream(buffer);
            }
            return stream;
        }
        public static VfsEntry Root()
        {
            return new VfsEntry("", false, 0, null, new VfsPhysicalDirectory(new DirectoryInfo("root")));
        }
    }
}