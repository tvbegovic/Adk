using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model
{
	public class Note
	{
		public int Id { get; set; }
		[ColumnNameAttr( "note_key" )]
		public string Key { get; set; }
		public string Content { get; set; }
		[ColumnNameAttr( "date_modified" )]
		public DateTime DateModified { get; set; }
		[ColumnNameAttr( "user_modified" )]
		public Guid UserModified { get; set; }

		public string DateModifiedString
		{
			get { return DateModified.ToString(); }
		}


		public static Note GetByKey( string key )
		{
			string query = "SELECT * FROM notes WHERE note_key = @key";
			var note = Database.ItemFetcher<Note>( query, "@key", key );
			//return empty note
			if ( note.Id == 0 ) {
				note.Key = key;
			}

			return note;
		}

		public static void InsertOrUpdate( string noteKey, string noteContent, string userId )
		{
			if ( String.IsNullOrWhiteSpace( noteKey ) ) {
				return;
			}

			string query = @"INSERT INTO notes (note_key, content, user_modified, date_modified) 
								  VALUES(@key, @content, @userId, @date) ON DUPLICATE KEY UPDATE 
								  content = @content, date_modified = @date, user_modified = @userId";
			Database.Insert( query, "@key", noteKey, "@content", noteContent, "@userId", userId, "@date", DateTime.Now );
		}

		public static void Delete( int noteId )
		{
			string query = @"DELETE FROM notes WHERE Id = @id";
			Database.Delete( query, "@id", noteId );
		}
	}
}