using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseCommon;
using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model
{
	public class DayPartSet
	{
		public int Id;
		public string UserId;
		public string Name;
		public List<DayPart> Parts = new List<DayPart>();

		public static List<DayPartSet> GetForUser( string userId)
		{
			using ( var conn = Database.Get() )
				return GetForUser( conn, null, userId );
		}
				

		public static List<DayPartSet> GetForUser( MySqlConnection conn, MySqlTransaction tran, string userId )
		{
			var cmd = new MySqlCommand( @"
				SELECT s.id, s.name, p.id, p.name, h.day, h.hour,p.short_code, p.color, p.background_color
				FROM day_part_set s
				LEFT JOIN day_part p ON s.id = p.day_part_set_id
				LEFT JOIN day_part_hour h ON p.id = h.day_part_id
				WHERE s.user_id = @userId
				ORDER BY s.id, p.id", conn, tran );
			cmd.Parameters.AddWithValue( "@userId", userId );

			var sets = new List<DayPartSet>();
			DayPartSet set = null;
			DayPart part = null;
			using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					int setId = dr.GetInt32( 0 );
					if ( set == null || set.Id != setId )
						sets.Add( set = new DayPartSet { Id = setId, Name = dr.GetString( 1 ), UserId = userId } );

					int? partId = dr.GetNullableInt( 2 );
					if ( partId.HasValue && (part == null || part.Id != partId.Value) )
						set.Parts.Add( part = new DayPart
						{
							Id = partId.Value,
							Name = dr.GetString( 3 ),
							Short_code = dr.GetNullableString(6),
							Color = dr.GetNullableString(7),
							BackgroundColor = dr.GetNullableString(8)
						} );

					if ( !dr.IsDBNull( 4 ) )
						part.Hours.Add( new DayHour { DayPartId = part.Id, Day = (DayOfWeek)dr.GetByte( 4 ), Hour = dr.GetByte( 5 ) } );
				}
			}

			return sets;
		}

		public void Create()
		{
			this.Id = (int)Database.Insert(
				@"INSERT INTO day_part_set (name, user_id) VALUES (@name, @user_id)",
				"@name", this.Name,
				"@user_id", this.UserId
			);
		}
		public bool Delete()
		{
			bool deleted = false;
			if ( this.Id != 0 ) {
				deleted = Database.Delete( "DELETE FROM day_part_set WHERE id = @id", "@id", this.Id );
				this.Id = 0;
			}
			return deleted;
		}
		public void Insert( MySqlConnection conn, MySqlTransaction tran, DayPart dayPart )
		{
			dayPart.Id = (int)Database.Insert( @"INSERT INTO day_part (day_part_set_id, name,short_code, color, background_color)
				VALUES (@day_part_set_id, @name,@short_code,@color, @backgroundColor)",
				"@day_part_set_id", this.Id,
				"@name", dayPart.Name,
                "@short_code", dayPart.Short_code,
                "@color", dayPart.Color,
				"@backgroundColor", dayPart.BackgroundColor
			);

			foreach ( var h in dayPart.Hours )
				h.Insert( conn, tran );
		}
		public bool Update( MySqlConnection conn, MySqlTransaction tran, DayPart dbPart, DayPart dayPart )
		{
			if ( dbPart.Name != dayPart.Name || dbPart.Short_code != dayPart.Short_code || dbPart.Color != dayPart.Color || dbPart.BackgroundColor != dayPart.BackgroundColor )
				Database.ExecuteNonQuery( conn, tran, @"UPDATE day_part SET name = @name, short_code = @short_code, color = @color,
					background_color = @backgroundColor WHERE id = @id",
					"@name", dayPart.Name,
					"@id", dayPart.Id,
                    "@short_code", dayPart.Short_code,
                    "@color", dayPart.Color,
					"@backgroundColor", dayPart.BackgroundColor
				);

			var removedHours = dbPart.Hours.Where( dh => !dayPart.Hours.Any( dh1 => dh1.Equals( dh ) ) ).ToList();
			foreach ( var dh in removedHours )
				dh.Delete( conn, tran );

			var insertedHours = dayPart.Hours.Where( dh => !dbPart.Hours.Any( dh1 => dh1.Equals( dh ) ) ).ToList();
			foreach ( var dh in insertedHours )
				dh.Insert( conn, tran );

			return true;
		}
		public bool DeletePart( int partId )
		{
			var dayPart = Parts.FirstOrDefault( p => p.Id == partId );
			if ( dayPart != null )
				return Database.Delete( "DELETE FROM day_part WHERE id = @id", "@id", partId );

			return false;
		}

		public static List<DayPartSet> GetForUser2(MySqlConnection conn, MySqlTransaction tran, string userId)
		{
			var cmd = new MySqlCommand(@"
				SELECT s.id, s.name, p.id, p.name, p.short_code, p.color,t.day, t.time_from, t.time_to
				FROM day_part_set s
				LEFT JOIN day_part p ON s.id = p.day_part_set_id
				LEFT JOIN day_part_time t ON p.id = t.day_part_id
				WHERE s.user_id = @userId
				ORDER BY s.id, p.id", conn, tran);
			cmd.Parameters.AddWithValue("@userId", userId);

			var sets = new List<DayPartSet>();
			DayPartSet set = null;
			DayPart part = null;
			using (var dr = cmd.ExecuteReader())
			{
				while (dr.Read())
				{
					int setId = dr.GetInt32(0);
					if (set == null || set.Id != setId)
						sets.Add(set = new DayPartSet { Id = setId, Name = dr.GetString(1), UserId = userId });

					int? partId = dr.GetNullableInt(2);
					if (partId.HasValue && (part == null || part.Id != partId.Value))
						set.Parts.Add(part = new DayPart { Id = partId.Value, Name = dr.GetString(3), Short_code = dr.GetNullableString(4), Color = dr.GetNullableString(5) });

					if (!dr.IsDBNull(6))
						part.Times.Add(new DayPartTime { DayPartId = part.Id, Day = (DayOfWeek)dr.GetByte(6), From = dr.GetDateTime(7), To = dr.GetDateTime(8) });
				}
			}

			return sets;
		}
	}
	public class DayPart
	{
		public int Id;
		public string Name;
        public string Short_code;
        public string Color;
		public string BackgroundColor;
		public List<DayHour> Hours = new List<DayHour>();
		public List<DayPartTime> Times = new List<DayPartTime>();

		public static List<DayPart> GetAllDayParts()
		{
			var dayParts = new List<DayPart>();

			for ( byte i = 0; i < 24; i++ ) {
                dayParts.Add(new DayPart {
                    Id = i,
					Name = String.Format( "{0}h", i ),
					Hours = new List<DayHour>
						{
							new DayHour { Hour = i }
						}
				} );
			}

			return dayParts;
		}
	}
	public class DayHour
	{
		public int DayPartId;
		public DayOfWeek Day;
		public byte Hour;

		public override bool Equals( object obj )
		{
			if ( obj == null || !(obj is DayHour) )
				return false;
			var other = obj as DayHour;
			return
				this.DayPartId == other.DayPartId &&
				this.Day == other.Day &&
				this.Hour == other.Hour;
		}
		public override int GetHashCode()
		{
			unchecked // http://stackoverflow.com/a/720282/183386
			{
				int hash = 27;
				hash = (13 * hash) + DayPartId.GetHashCode();
				hash = (13 * hash) + Day.GetHashCode();
				hash = (13 * hash) + Hour.GetHashCode();
				return hash;
			}
		}

		public long Insert( MySqlConnection conn, MySqlTransaction tran )
		{
			return Database.Insert( conn, tran, @"INSERT INTO day_part_hour (day_part_id, day, hour) VALUES (@dayPartId, @day, @hour)",
				"@dayPartId", DayPartId,
				"@day", (byte)Day,
				"@hour", Hour );
		}
		public bool Delete( MySqlConnection conn, MySqlTransaction tran )
		{
			return Database.Delete( conn, tran, "DELETE from day_part_hour WHERE day_part_id = @dayPartId AND day = @day AND hour = hour",
				"@dayPartId", DayPartId,
				"@day", (byte)Day,
				"@hour", Hour );
		}
	}

	public class DayPartTime
	{
		public int Id;
		public int DayPartId;
		public DayOfWeek Day;
		public DateTime? From;
		public DateTime? To;

		public long Insert(MySqlConnection conn, MySqlTransaction tran)
		{
			return Database.Insert(conn, tran, @"INSERT INTO `day_part_time`
									(`id`,`day_part_id`,`day`,`time_from`,`time_to`)
									VALUES
									(@id,@day_part_id,@day,@time_from,@time_to);",
				"@day_part_id", DayPartId, "@day", (byte)Day,"@time_from", From, "@time_to", To);
		}

		public int Update(MySqlConnection conn, MySqlTransaction tran)
		{
			return Database.ExecuteNonQuery(conn, tran,
				@"UPDATE `day_part_time` SET
				`id` = @id,`day_part_id` = @day_part_id,`day` = @day,`time_from` = @time_from,
				`time_to` = @time_to WHERE `id` = @id;",
				"@day_part_id", DayPartId, "@day", (byte)Day, "@time_from", From, "@time_to", To, "@id", Id
				);
		}

		public bool Delete(MySqlConnection conn, MySqlTransaction tran)
		{
			return Database.Delete(conn, tran, "DELETE from day_part_time WHERE id = @id","@id", Id);
		}

	}
}
