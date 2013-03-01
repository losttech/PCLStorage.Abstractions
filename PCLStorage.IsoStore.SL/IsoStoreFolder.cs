﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Linq;

namespace PCLStorage
{
	public class IsoStoreFolder : IFolder
	{
		internal IsolatedStorageFile Root { get; private set; }

		readonly string _name;
		readonly string _path;

		public IsoStoreFolder()
			: this (IsolatedStorageFile.GetUserStoreForApplication())
		{
		}

		public IsoStoreFolder(IsolatedStorageFile root)
		{
			Root = root;
			_name = string.Empty;
			_path = string.Empty;
		}

		public IsoStoreFolder(string name, IsoStoreFolder parent)
		{
			Root = parent.Root;
			_name = name;
			_path = System.IO.Path.Combine(parent.Path, _name);
		}

		public string Name
		{
			get { return _name; }
		}

		public string Path
		{
			get { return _path; }
		}

		public Task<IFile> CreateFileAsync(string desiredName, CreationCollisionOption option)
		{
			string nameToUse = desiredName;
			string path = System.IO.Path.Combine(Path, nameToUse);
			if (Root.FileExists(path))
			{
				if (option == CreationCollisionOption.GenerateUniqueName)
				{
					for (int num = 1; Root.FileExists(path) ; num++)
					{
						nameToUse = desiredName + "(" + num + ")";
						path = System.IO.Path.Combine(Path, nameToUse);
					}
				}
				else if (option == CreationCollisionOption.ReplaceExisting)
				{
					Root.DeleteFile(path);
					using (Root.CreateFile(path)) { }
				}
				else if (option == CreationCollisionOption.FailIfExists)
				{
					throw new IOException("File already exists: " + path);
				}
				else if (option == CreationCollisionOption.OpenIfExists)
				{
					//	No operation
				}
				else
				{
					throw new ArgumentException("Unrecognized CreationCollisionOption: " + option);
				}
			}
			else
			{
				//	Create file
				using (Root.CreateFile(path)) { }
			}

			var ret = new IsoStoreFile(nameToUse, this);
			return TaskEx.FromResult<IFile>(ret);
		}

		public Task<IFile> GetFileAsync(string name)
		{
			string path = System.IO.Path.Combine(Path, name);
			if (!Root.FileExists(path))
			{
				throw new FileNotFoundException("File does not exist: " + path);
			}
			var ret = new IsoStoreFile(name, this);
			return TaskEx.FromResult<IFile>(ret);
		}

		public Task<IList<IFile>> GetFilesAsync()
		{
			string[] fileNames = Root.GetFileNames(System.IO.Path.Combine(Path, "*.*"));
			IList<IFile> ret = fileNames.Select(fn => new IsoStoreFile(fn, this)).Cast<IFile>().ToList().AsReadOnly();
			return TaskEx.FromResult(ret);
		}

		public Task<IFolder> CreateFolderAsync(string desiredName, CreationCollisionOption option)
		{
			string nameToUse = desiredName;
			string path = System.IO.Path.Combine(Path, nameToUse);
			if (Root.DirectoryExists(path))
			{
				if (option == CreationCollisionOption.GenerateUniqueName)
				{
					for (int num = 1; Root.DirectoryExists(path); num++)
					{
						nameToUse = desiredName + "(" + num + ")";
						path = System.IO.Path.Combine(Path, nameToUse);
					}
				}
				else if (option == CreationCollisionOption.ReplaceExisting)
				{
					Root.DeleteDirectory(path);
					Root.CreateDirectory(path);
				}
				else if (option == CreationCollisionOption.FailIfExists)
				{
					throw new IOException("Directory already exists: " + path);
				}
				else if (option == CreationCollisionOption.OpenIfExists)
				{
					//	No operation
				}
				else
				{
					throw new ArgumentException("Unrecognized CreationCollisionOption: " + option);
				}
			}
			else
			{
				Root.CreateDirectory(path);
			}

			var ret = new IsoStoreFolder(nameToUse, this);
			return TaskEx.FromResult<IFolder>(ret);
		}

		public Task<IFolder> GetFolderAsync(string name)
		{
			string path = System.IO.Path.Combine(Path, name);
			if (!Root.DirectoryExists(path))
			{
				throw new DirectoryNotFoundException("Directory does not exist: " + path);
			}
			var ret = new IsoStoreFolder(name, this);
			return TaskEx.FromResult<IFolder>(ret);
		}

		public Task<IList<IFolder>> GetFoldersAsync()
		{
			string[] folderNames = Root.GetDirectoryNames(System.IO.Path.Combine(Path, "*"));
			IList<IFolder> ret = folderNames.Select(fn => new IsoStoreFolder(fn, this)).Cast<IFolder>().ToList().AsReadOnly();
			return TaskEx.FromResult(ret);
		}

		public Task DeleteAsync()
		{
			Root.DeleteDirectory(Path);
			return TaskEx.FromResult(true);
		}
	}
}