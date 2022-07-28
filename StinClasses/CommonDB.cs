using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StinClasses
{
    public static class CommonDB
    {
        public static DateTime GetDateTA(this StinDbContext context)
        {
            return context._1ssystems.OrderBy(x => x.Curdate).First().Curdate;
        }
        public static DateTime GetDateTimeTA(this StinDbContext context)
        {
            var systemEntity = context._1ssystems.OrderBy(x => x.Curdate).FirstOrDefault();
            if (systemEntity != null)
            {
                var milliSeconds = systemEntity.Curtime / 10; 
                return systemEntity.Curdate.AddMilliseconds(milliSeconds);
            }
            return DateTime.MinValue;
        }
        public static DateTime GetRegTA(this StinDbContext context)
        {
            DateTime DateTA = GetDateTA(context);
            return new DateTime(DateTA.Year, DateTA.Month, 1);
        }
        public static string ПрефиксИБ(this StinDbContext context, string userId = null, bool alt = false)
        {
            string ПрефиксИБ = "";
            if (!string.IsNullOrEmpty(userId))
            {
                var result = (from sc30 in context.Sc30s
                              join sc9506 in context.Sc9506s on sc30.Sp11726 equals sc9506.Id into _sc9506
                              from sc9506 in _sc9506.DefaultIfEmpty()
                              where sc30.Id == userId
                              select new
                              {
                                  ПрефиксИБ = sc9506 != null ? sc9506.Sp9504.Trim() : "",
                                  ПрефиксИБАльт = sc9506 != null ? sc9506.Sp12925.Trim() : ""
                              }).FirstOrDefault();
                ПрефиксИБ = (alt && result.ПрефиксИБАльт != "") ? result.ПрефиксИБАльт : result.ПрефиксИБ;
            }
            if (string.IsNullOrEmpty(ПрефиксИБ))
                ПрефиксИБ = (from _const in context._1sconsts
                             where _const.Id == 3701 && _const.Objid == "     0   " && _const.Date <= Common.min1cDate
                             orderby _const.Id descending, _const.Objid descending, _const.Date descending, _const.Time descending, _const.Docid descending
                             select _const.Value).FirstOrDefault().Trim();
            return ПрефиксИБ;
        }
        public static string GenerateId(this StinDbContext context, int ВидСпрИД_dds)
        {
            _1suidctl ent = (from idCtl in context._1suidctls
                             where idCtl.Typeid == ВидСпрИД_dds
                             select idCtl)
                             .OrderBy(x => x.Maxid)
                             .FirstOrDefault();
            if (ent == null)
                ent = new _1suidctl { Typeid = ВидСпрИД_dds, Maxid = Common.ПустоеЗначение };
            string MaxId = ent.Maxid.Substring(0, 6).Trim();
            long num10 = Common.Decode36(MaxId) + 1;

            ent.Maxid = (Common.Encode36(num10) + ПрефиксИБ(context).PadRight(3)).PadLeft(9);
            if (num10 == 1)
                context._1suidctls.Add(ent);
            else
                context.Update(ent);

            return ent.Maxid;
        }
        public static void РегистрацияИзмененийРаспределеннойИБ(this StinDbContext context, int ВидДокИД_dds, string IdDoc)
        {
            string prefix = context.ПрефиксИБ();
            var signs = (from dbset in context._1sdbsets
                         where dbset.Dbsign.Trim() != prefix
                         select dbset.Dbsign).ToList();
            foreach (var sign in signs)
                context.Database.ExecuteSqlRaw("exec _1sp_RegisterUpdate @sign,@doc_dds,@num36,' '", new SqlParameter("@sign", sign), new SqlParameter("@doc_dds", ВидДокИД_dds), new SqlParameter("@num36", IdDoc));
        }
        public static async Task<string> ПолучитьЗначениеПериодическогоРеквизита(this StinDbContext context, string objId, int Id)
        {
            return await (from _const in context._1sconsts
                          where _const.Objid == objId && _const.Id == Id
                          orderby _const.Date descending, _const.Time descending, _const.Docid descending
                          select _const.Value).FirstOrDefaultAsync();
        }
        public static _1sconst ИзменитьПериодическиеРеквизиты(this StinDbContext context, string objId, int реквизитDds,
            string docId, DateTime dateTime, string value, int actNo, short lineNo = 0)
        {
            return new _1sconst
            {
                Objid = objId,//для транспортных маршрутов это Id элемента справочника ТранспортныеМаршруты
                Id = реквизитDds,
                Date = dateTime.Date,
                Time = (dateTime.Hour * 3600 * 10000) + (dateTime.Minute * 60 * 10000) + (dateTime.Second * 10000),
                Docid = docId,
                Value = value,//маршрут "э7777" или " 1W9 1TRBVS  " для документа ЗаявкаПокупателя с docId = " 1TRBVS  "
                Actno = actNo,//номер движения документа
                Lineno = lineNo,//Номер строки документа (заполняется при вызове метода ПривязыватьСтроку(), если привязка не выполнена или непериодическое значение - заполняется нулем
                Tvalue = "", //Заполняется только для неопределенных реквизитов, для типов данных 1С (когда длина ID равна 23 символам)
            };
        }
        public static void ОбновитьПериодическиеРеквизиты(this StinDbContext context, string objId, int реквизитDds, string docId, string value)
        {
            var entity = context._1sconsts
                .FirstOrDefault(x => (x.Objid == objId) && (x.Id == реквизитDds) && (x.Docid == docId));
            if (entity != null)
            {
                entity.Value = value;
                context.Update(entity);
                context.SaveChanges();
            }
        }
        public static void ОбновитьСетевуюАктивность(this StinDbContext context)
        {
            int СчетчикАктивности = context._1susers.Select(x => x.Netchgcn).FirstOrDefault();
            СчетчикАктивности++;
            context.Database.ExecuteSqlRaw("Update _1SUSERS set NETCHGCN=" + СчетчикАктивности.ToString());
        }
    }
}
