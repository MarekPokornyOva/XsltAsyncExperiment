#region using
using System;
using System.Threading;
using System.Xml;
#endregion

namespace XsltCancellation
{
	public class CancellableXmlWriter:XmlWriter
	{
		readonly XmlWriter _inner;
		readonly CancellationToken _cancellationToken;
		readonly bool _log;
		public CancellableXmlWriter(XmlWriter inner,CancellationToken cancellationToken,bool log)
		{
			_inner=inner;
			_cancellationToken=cancellationToken;
			_log=log;
		}

		public override WriteState WriteState => throw new NotImplementedException();

		public override void Flush()
		{
			_cancellationToken.ThrowIfCancellationRequested();
			_inner.Flush();
		}

		public override string LookupPrefix(string ns)
		{
			throw new NotImplementedException();
		}

		public override void WriteBase64(byte[] buffer,int index,int count)
		{
			throw new NotImplementedException();
		}

		public override void WriteCData(string text)
		{
			throw new NotImplementedException();
		}

		public override void WriteCharEntity(char ch)
		{
			throw new NotImplementedException();
		}

		public override void WriteChars(char[] buffer,int index,int count)
		{
			throw new NotImplementedException();
		}

		public override void WriteComment(string text)
		{
			throw new NotImplementedException();
		}

		public override void WriteDocType(string name,string pubid,string sysid,string subset)
		{
			throw new NotImplementedException();
		}

		public override void WriteEndAttribute()
		{
			_cancellationToken.ThrowIfCancellationRequested();
			_inner.WriteEndAttribute();
		}

		public override void WriteEndDocument()
		{
			throw new NotImplementedException();
		}

		public override void WriteEndElement()
		{
			_cancellationToken.ThrowIfCancellationRequested();
			if (_log)
				Console.WriteLine($"</>");
			_inner.WriteEndElement();
		}

		public override void WriteEntityRef(string name)
		{
			throw new NotImplementedException();
		}

		public override void WriteFullEndElement()
		{
			throw new NotImplementedException();
		}

		public override void WriteProcessingInstruction(string name,string text)
		{
			throw new NotImplementedException();
		}

		public override void WriteRaw(char[] buffer,int index,int count)
		{
			throw new NotImplementedException();
		}

		public override void WriteRaw(string data)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			_inner.WriteRaw(data);
		}

		public override void WriteStartAttribute(string prefix,string localName,string ns)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			_inner.WriteStartAttribute(prefix,localName,ns);
		}

		public override void WriteStartDocument()
		{
			throw new NotImplementedException();
		}

		public override void WriteStartDocument(bool standalone)
		{
			throw new NotImplementedException();
		}

		public override void WriteStartElement(string prefix,string localName,string ns)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			if (_log)
				Console.WriteLine($"<{localName}>");
			_inner.WriteStartElement(prefix,localName,ns);
		}

		public override void WriteString(string text)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			if (_log)
				Console.WriteLine(text);
			_inner.WriteString(text);
		}

		public override void WriteSurrogateCharEntity(char lowChar,char highChar)
		{
			throw new NotImplementedException();
		}

		public override void WriteWhitespace(string ws)
		{
			throw new NotImplementedException();
		}
	}
}