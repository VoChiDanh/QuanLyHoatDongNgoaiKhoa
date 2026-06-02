using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using System.Web;

namespace QL_HDNK.Models
{
    public partial class QuanLy_HDNKEntities
    {
        public override int SaveChanges()
        {
            // 1. Lấy danh sách các thay đổi
            var entries = this.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            if (entries.Any())
            {
                string userId = null;
                try
                {
                    if (HttpContext.Current != null)
                    {
                        if (HttpContext.Current.Session != null)
                        {
                            userId = HttpContext.Current.Session["MaNguoiDung"] as string;
                        }

                        if (string.IsNullOrEmpty(userId) && HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
                        {
                            userId = HttpContext.Current.User.Identity.Name;
                        }
                    }
                }
                catch { }

                var logsToAdd = new List<NhatKyHeThong>();

                foreach (var entry in entries)
                {
                    var entityType = entry.Entity.GetType();
                    // Xử lý proxy của EF để lấy type thật
                    if (entityType.BaseType != null && entityType.Namespace == "System.Data.Entity.DynamicProxies")
                    {
                        entityType = entityType.BaseType;
                    }

                    string tableName = entityType.Name;
                    if (tableName == "NhatKyHeThong") continue;

                    string keyInfo = "";
                    try
                    {
                        var objectContext = ((IObjectContextAdapter)this).ObjectContext;
                        // Sử dụng reflection để gọi CreateObjectSet<T>() vì entityType chỉ biết ở runtime
                        var method = objectContext.GetType().GetMethods()
                            .FirstOrDefault(m => m.Name == "CreateObjectSet" && m.IsGenericMethod && m.GetParameters().Length == 0);
                        
                        if (method != null)
                        {
                            var genericMethod = method.MakeGenericMethod(entityType);
                            dynamic set = genericMethod.Invoke(objectContext, null);
                            
                            var keys = new List<string>();
                            foreach (var key in set.EntitySet.ElementType.KeyMembers)
                            {
                                string name = key.Name;
                                var val = entry.State == EntityState.Added ? entry.CurrentValues[name] : entry.OriginalValues[name];
                                keys.Add($"{name}={val}");
                            }
                            keyInfo = "[" + string.Join(", ", keys) + "]";
                        }
                    }
                    catch { }

                    string detail = "";
                    if (entry.State == EntityState.Added)
                    {
                        detail = $"Thêm mới {tableName} {keyInfo}: ";
                        foreach (var prop in entry.CurrentValues.PropertyNames)
                        {
                            var val = entry.CurrentValues[prop];
                            if (val != null) detail += $"{prop}={val}; ";
                        }
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        detail = $"Sửa {tableName} {keyInfo}: ";
                        foreach (var prop in entry.OriginalValues.PropertyNames)
                        {
                            var original = entry.OriginalValues[prop];
                            var current = entry.CurrentValues[prop];
                            if (!object.Equals(original, current))
                            {
                                detail += $"{prop}: {original} -> {current}; ";
                            }
                        }
                        if (detail == $"Sửa {tableName} {keyInfo}: ") continue;
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        detail = $"Xóa {tableName} {keyInfo}: ";
                        foreach (var prop in entry.OriginalValues.PropertyNames)
                        {
                            var val = entry.OriginalValues[prop];
                            if (val != null) detail += $"{prop}={val}; ";
                        }
                    }

                    if (!string.IsNullOrEmpty(detail))
                    {
                        logsToAdd.Add(new NhatKyHeThong
                        {
                            // Sử dụng 10 ký tự GUID nhưng đảm bảo không trùng
                            MaNhatKy = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                            MaNguoiDung = userId,
                            ChiTiet = detail.Length > 4000 ? detail.Substring(0, 3997) + "..." : detail,
                            ThoiGian = DateTime.Now
                        });
                    }
                }

                if (logsToAdd.Any())
                {
                    this.NhatKyHeThongs.AddRange(logsToAdd);
                }
            }

            return base.SaveChanges();
        }
    }
}
