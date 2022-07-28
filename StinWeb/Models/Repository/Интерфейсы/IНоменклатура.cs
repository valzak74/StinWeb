using StinWeb.Models.DataManager.Справочники;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StinClasses.Models;

namespace StinWeb.Models.Repository.Интерфейсы
{
    public interface IНоменклатура: IDisposable
    {
        IQueryable<Sc84> GetAll();
        Sc84 GetById(string Id);
        Task<Sc84> GetByIdAsync(string Id);
        Task<Номенклатура> GetНоменклатураByIdAsync(string Id);
        IQueryable<Производитель> GetAllBrends();
        public IQueryable<string> GetBrendsForFilter(РежимВыбора режим);
        IQueryable<Номенклатура> GetAllWithBrend();
        IQueryable<Номенклатура> GetAllWithBrendБезПапкиЗапчасти();
        IQueryable<Номенклатура> GetAllWithBrendWithCost(string parentId);
        IQueryable<Номенклатура> ПолучитьНоменклатуруПоАгентуПроизводителя(string контрагентId, IEnumerable<string> списокНоменклатурыId);
        IQueryable<Номенклатура> ВсяНоменклатураБезПапокБрендЦена();
        Task<IQueryable<Номенклатура>> НоменклатураЦенаКлиентаAsync(List<string> Ids, string договорId, string картаId, bool доставка, int типДоставки, string search);
        Task<IQueryable<Номенклатура>> ВсяНоменклатураБезПапокБрендЦенаОстаткиAsync(List<string> СписокФирмId, string складId, string договорId, string картаId, bool доставка, int типДоставки, string search);
        IQueryable<Номенклатура> ВсяНоменклатураБезПапок();
        IEnumerable<Номенклатура> ПолучитьНоменклатуруПоАртикулу(string артикул);
        IEnumerable<Номенклатура> ПолучитьНоменклатуруПоНаименованию(string наименование);
        IEnumerable<Номенклатура> ПолучитьНоменклатуруПоШтрихКоду(string штрихКод);
        Task<Единицы> ОсновнаяЕдиницаAsync(string номенклатураId);
        Task<СтавкаНДС> СтавкаНДСAsync(string номенклатураId);
        Task<List<Номенклатура>> АналогиНоменклатурыAsync(string номенклатураId);
        IQueryable<decimal> Остаток(string Id);
        Task<decimal> ОстатокAsync(string Id);
        Task<(string НоменклатураId, string Message)> CreateNewAsync(bool Testing, string ParentId, string BrendId, string ManagerId,
            string Aртикул, string Наименование, string ЕдиницаНазвание, string Штрихкод,
            bool СнятСПроизводства, bool ПодЗаказ, bool Web,
            string Комментарий, string КраткоеОписание, string ПодробноеОписание, string Характеристики,
            string АртикулОригинал, string ТнВЭД,
            decimal Вес, decimal ВесБрутто, decimal КолМест, decimal Ширина, decimal Высота, decimal Глубина);
        Task<string> UpdateAsync(bool Testing, string Id, string ManagerId, string Артикул,
            bool NeedUpdateНаименование, string Наименование,
            bool NeedUpdateШтрихкод, string Штрихкод,
            bool NeedUpdateСнятСПроизводства, bool СнятСПроизводства,
            bool NeedUpdateПодЗаказ, bool ПодЗаказ,
            bool NeedUpdateWeb, bool Web,
            bool NeedUpdateКомментарий, string Комментарий,
            bool NeedUpdateКраткоеОписание, string КраткоеОписание,
            bool NeedUpdateПодробноеОписание, string ПодробноеОписание,
            bool NeedUpdateХарактеристики, string Характеристики,
            bool NeedUpdateАртикулОригинал, string АртикулОригинал,
            bool NeedUpdateТнВэд, string ТнВЭД,
            bool NeedUpdateВес, decimal Вес,
            bool NeedUpdateВесБрутто, decimal ВесБрутто,
            bool NeedUpdateКолМест, decimal КолМест,
            bool NeedUpdateШирина, decimal Ширина,
            bool NeedUpdateВысота, decimal Высота,
            bool NeedUpdateГлубина, decimal Глубина
            );
        IQueryable<VzTovarImage> GetImages(string Id);
        Task<byte[]> GetImageAsync(int rowId);
        IQueryable<ТаблицаСвободныхОстатков> ПодготовитьОстатки(DateTime RegDate, List<string> списокФирмId, List<string> списокСкладовId, List<string> списокТоваровId);
        IQueryable<ТаблицаСвободныхОстатков> ПодготовитьСвободныеОстатки(DateTime RegDate, List<string> списокФирмId, List<string> списокСкладовId, List<string> списокТоваровId);
    }
}
