using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Adk.Handlers
{
	public class AudioExport
	{
		public int AudioBitrate { get; internal set; }
		public int AudioSampleRate { get; internal set; }
		public int Channels { get; internal set; }
		public Stream OutputStream { get; internal set; }
		private List<string> files = new List<string>();

		internal void AddFile(string filename, double cutStart, double cutEnd)
		{
			files.Add(filename);
		}

		internal void Export()
		{
			throw new NotImplementedException();
		}

		internal void Close()
		{
			throw new NotImplementedException();
		}
	}
}