using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NonFactors.Mvc.Lookup;
using System.ComponentModel.DataAnnotations;

namespace StinWeb.Models.DataManager.Справочники
{
    public class ExceptionData
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public bool Skip { get; set; }
    }
    public class Маршрут
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Наименование { get; set; }
    }
    public class Телефон
    {
        [Key]
        [LookupColumn(Hidden = true)]
        public string Id { get; set; }
        public string КонтрагентId { get; set; }
        [LookupColumn]
        public string Номер { get; set; }
    }
    public class Email
    {
        [Key]
        [LookupColumn(Hidden = true)]
        public string Id { get; set; }
        public string КонтрагентId { get; set; }
        [LookupColumn]
        public string Адрес { get; set; }
    }
    public class BinaryData
    {
        public string Id { get; set; }
        public string FileExtension { get; set; }
        public byte[] Body { get; set; }
    }
    public enum РежимВыбора : short
    {
        Общий = 0,
        ПоМастерской = 1,
        ПоТовару = 2
    }
}
