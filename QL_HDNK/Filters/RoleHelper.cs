using System.Globalization;
using System.Text;

namespace QL_HDNK.Filters
{
    public static class RoleHelper
    {
        public static string Normalize(string roleCode, string roleName)
        {
            var raw = RemoveDiacritics(((roleCode ?? "") + " " + (roleName ?? "")).ToLowerInvariant());

            if (ContainsAny(raw, "admin", "rolekeys.admin", "qtv", "quan tri"))
            {
                return RoleKeys.Admin;
            }

            if (ContainsAny(raw,
                "facultyunion", "rolekeys.facultyunion",
                "bch khoa", "bch doan", "bch lch",
                "ban chap hanh khoa", "ban chap hanh doan",
                "doan khoa", "doan hoi",
                "lien chi", "lch",
                "quan ly khoa", "khoa"))
            {
                return RoleKeys.FacultyUnion;
            }

            if (ContainsAny(raw,
                "classofficer", "rolekeys.classofficer",
                "bcs", "bch",
                "ban can su", "ban chap hanh",
                "lop", "bi thu"))
            {
                return RoleKeys.ClassOfficer;
            }

            return RoleKeys.Student;
        }

        public static bool IsAtLeast(string currentRole, string requiredRole)
        {
            return Rank(currentRole) >= Rank(requiredRole);
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            foreach (var needle in needles)
            {
                if (value.Contains(needle))
                {
                    return true;
                }
            }

            return false;
        }

        private static string RemoveDiacritics(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString()
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .Normalize(NormalizationForm.FormC);
        }

        private static int Rank(string role)
        {
            switch (role)
            {
                case RoleKeys.Admin:
                    return 4;
                case RoleKeys.FacultyUnion:
                    return 3;
                case RoleKeys.ClassOfficer:
                    return 2;
                case RoleKeys.Student:
                    return 1;
                default:
                    return 0;
            }
        }
    }
}
