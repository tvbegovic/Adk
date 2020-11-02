using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DatabaseCommon
{
	public class SettingItem
	{
		public string Module { get; set; }
		public string Key { get; set; }
		public string Value { get; set; }

	}

	public class UserSettingItem : SettingItem
	{
		public string UserId { get; set; }
	}
}
