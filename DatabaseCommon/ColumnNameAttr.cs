using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DatabaseCommon
{
	/// <summary>
	/// Attribute used by AdoNet auto binder extension
	/// </summary>
	public class ColumnNameAttr : System.Attribute
	{
		private readonly string _name;
		public string Name
		{
			get { return _name; }
		}

		public ColumnNameAttr( string name )
		{
			this._name = name;
		}
	}

}
