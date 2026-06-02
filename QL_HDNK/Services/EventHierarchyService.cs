using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using QL_HDNK.Models;

namespace QL_HDNK.Services
{
    public class EventParentInfo
    {
        public string MaSuKien { get; set; }
        public string MaSuKienCha { get; set; }
    }

    public class ParentEventOption
    {
        public string MaSuKien { get; set; }
        public string TenSuKien { get; set; }
        public string DisplayText
        {
            get { return MaSuKien + " - " + TenSuKien; }
        }
    }

    public static class EventHierarchyService
    {
        public static void EnsureSchema(QuanLy_HDNKEntities db)
        {
            db.Database.ExecuteSqlCommand(@"
IF COL_LENGTH('dbo.SuKien', 'MaSuKienCha') IS NULL
BEGIN
    ALTER TABLE dbo.SuKien ADD MaSuKienCha nvarchar(10) NULL;
END");
        }

        public static string GetParentId(QuanLy_HDNKEntities db, string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return null;
            }

            EnsureSchema(db);
            return db.Database.SqlQuery<string>(
                "SELECT MaSuKienCha FROM dbo.SuKien WHERE MaSuKien = @id",
                new SqlParameter("@id", eventId)).FirstOrDefault();
        }

        public static void SaveParentId(QuanLy_HDNKEntities db, string eventId, string parentId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return;
            }

            EnsureSchema(db);
            parentId = string.IsNullOrWhiteSpace(parentId) || parentId == eventId ? null : parentId.Trim();
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.SuKien SET MaSuKienCha = @parentId WHERE MaSuKien = @eventId",
                new SqlParameter("@parentId", (object)parentId ?? System.DBNull.Value),
                new SqlParameter("@eventId", eventId));
        }

        public static List<EventParentInfo> LoadParentMap(QuanLy_HDNKEntities db, IEnumerable<string> eventIds)
        {
            EnsureSchema(db);
            var ids = eventIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (!ids.Any())
            {
                return new List<EventParentInfo>();
            }

            var parameters = ids.Select((id, index) => new SqlParameter("@id" + index, id)).ToArray();
            var names = string.Join(",", parameters.Select(x => x.ParameterName));
            return db.Database.SqlQuery<EventParentInfo>(
                "SELECT MaSuKien, MaSuKienCha FROM dbo.SuKien WHERE MaSuKien IN (" + names + ")",
                parameters).ToList();
        }
    }
}
