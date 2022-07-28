using System.Threading.Tasks;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Справочники;
using StinClasses.Models;

namespace StinWeb.Models.DataManager
{
    public static class Mapper
    {
        public static async Task<User> MapAsync(this Sc30 entity, StinDbContext context)
        {
            if (entity != null)
                return new User
                {
                    RowId = entity.RowId,
                    Id = entity.Id,
                    Name = entity.Code.Trim(),
                    Password = entity.Sp13677.Trim(),
                    FullName = entity.Descr.Trim(),
                    Department = entity.Sp13679.Trim(),
                    Role = entity.Sp13678.Trim(),
                    ОсновнойСклад = await new СкладRepository(context).GetEntityByIdAsync(entity.Sp873)
                };
            else
                return null;
        }
        public static Склад Map(this Sc55 entity)
        {
            if (entity != null)
                return new Склад
                {
                    RowId = entity.RowId,
                    Id = entity.Id,
                    Code = entity.Code.Trim(),
                    Наименование = entity.Descr.Trim(),
                };
            else
                return null;
        }
        public static ПодСклад Map(this Sc8963 entity)
        {
            if (entity != null)
                return new ПодСклад
                {
                    RowId = entity.RowId,
                    Id = entity.Id,
                    Наименование = entity.Descr.Trim(),
                };
            else
                return null;
        }
        public static Номенклатура Map(this Sc84 entity)
        {
            if (entity != null)
                return new Номенклатура
                {
                    Id = entity.Id,
                    ParentId = entity.Parentid,
                    IsFolder = entity.Isfolder == 1,
                    Code = entity.Code.Trim(),
                    Наименование = entity.Descr.Trim(),
                    Артикул = entity.Sp85.Trim(),
                    ПроизводительId = entity.Sp8842,
                };
            else
                return null;
        }
        public static Номенклатура Map(this Sc84 entity, Sc8840 brendEntity)
        {
            if (entity != null)
                return new Номенклатура
                {
                    Id = entity.Id,
                    ParentId = entity.Parentid,
                    IsFolder = entity.Isfolder == 1,
                    Code = entity.Code.Trim(),
                    Наименование = entity.Descr.Trim(),
                    Артикул = entity.Sp85.Trim(),
                    ПроизводительId = entity.Sp8842,
                    Производитель = brendEntity != null ? brendEntity.Descr.Trim() : "<не указан>"
                };
            else
                return null;
        }
        public static Номенклатура Map(this Sc84 entity, Sc8840 brendEntity, VzTovar vzTovarEntity)
        {
            if (entity != null)
                return new Номенклатура
                {
                    Id = entity.Id,
                    ParentId = entity.Parentid,
                    IsFolder = entity.Isfolder == 1,
                    Code = entity.Code.Trim(),
                    Наименование = entity.Descr.Trim(),
                    Артикул = entity.Sp85.Trim(),
                    ПроизводительId = entity.Sp8842,
                    Производитель = brendEntity != null ? brendEntity.Descr.Trim() : "<не указан>",
                    Цена = new Цены { Оптовая = vzTovarEntity != null ? vzTovarEntity.Opt ?? 0 : 0 }
                };
            else
                return null;
        }
        public static Производитель Map(this Sc8840 entity)
        {
            if (entity != null)
                return new Производитель
                {
                    Id = entity.Id,
                    Наименование = entity.Descr.Trim()
                };
            else
                return null;
        }
        public static Фирма Map(this Sc4014 entity)
        {
            if (entity != null)
                return new Фирма
                {
                    Id = entity.Id,
                    Наименование = entity.Descr.Trim(),
                    ЮрЛицо = new ЮрЛицо { Id = entity.Sp4011 },
                    Счет = new БанковскийСчет { Id = entity.Sp4133 },
                };
            else
                return null;
        }
        public static Фирма Map(this Sc4014 entity, Sc131 своиЮрЛица)
        {
            if (entity != null)
                return new Фирма
                {
                    Id = entity.Id,
                    Наименование = entity.Descr.Trim(),
                    ЮрЛицо = new ЮрЛицо 
                    { 
                        Id = entity.Sp4011,
                        Наименование = своиЮрЛица != null ? своиЮрЛица.Descr.Trim() : "<не указан>",
                        ИНН = своиЮрЛица != null ? своиЮрЛица.Sp135.Trim() : "<не указан>",
                        Префикс = своиЮрЛица != null ? своиЮрЛица.Sp145.Trim() : "",
                        УчитыватьНДС = своиЮрЛица != null ? своиЮрЛица.Sp4828 : 1,
                        Адрес = своиЮрЛица != null ? своиЮрЛица.Sp149.Trim() : "<не указан>",
                    },
                    Счет = new БанковскийСчет { Id = entity.Sp4133 },
                };
            else
                return null;
        }
        public static Контрагент Map(this Sc172 entity)
        {
            if (entity != null)
                return new Контрагент
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    Наименование = entity.Descr.Trim(),
                    ИНН = entity.Sp8380,
                    ОсновнойДоговор = entity.Sp667,
                    ГруппаКонтрагентов = entity.Sp9631
                };
            else
                return null;
        }
        public static Неисправность Map(this Sc9866 entity)
        {
            if (entity != null)
                return new Неисправность
                {
                    Id = entity.Id,
                    Наименование = entity.Descr.Trim(),
                };
            else
                return null;
        }
    }
}
