using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StinWeb.Models.DataManager.Справочники;
using StinWeb.Models.Repository.Справочники;
using StinClasses.Models;

namespace StinWeb.ViewComponents
{
    public class ДанныеИзменениеЦены
    {
        public Номенклатура Номенклатура { get; set; } 
        public decimal ТекущаяЦена { get; set; }
        public decimal РозничнаяЦена { get; set; }
        public decimal ОсобаяЦена { get; set; }
        public decimal СПрознЦена { get; set; }
        public decimal ОптоваяЦена { get; set; }
        public decimal СПоптЦена { get; set; }
        public decimal ПороговаяЦена { get; set; }
        public int Вариант { get; set; }
        public SelectList Варианты { get; set; }
    }
    public class ИзменениеЦеныViewComponent : ViewComponent
    {
        private НоменклатураRepository _номенклатураRepository;
        public ИзменениеЦеныViewComponent(StinDbContext context)
        {
            _номенклатураRepository = new НоменклатураRepository(context);
        }
        public async Task<IViewComponentResult> InvokeAsync(string номенклатураId, string текущееЗначение, string договорId, string типЦен, string картаId, bool доставка, int типДоставки)
        {
            ДанныеИзменениеЦены data = new ДанныеИзменениеЦены();
            Dictionary<int, string> варианты = new Dictionary<int, string>();
            текущееЗначение = string.IsNullOrEmpty(текущееЗначение) ? "0" : текущееЗначение.Replace('_', ' ');
            варианты.Add(0, "Текущее значение = " + текущееЗначение);
            data.ТекущаяЦена = decimal.Parse(текущееЗначение, NumberStyles.AllowCurrencySymbol | NumberStyles.Number);
            if (!string.IsNullOrEmpty(номенклатураId))
            {
                номенклатураId = номенклатураId.Replace('_', ' ');
                договорId = string.IsNullOrEmpty(договорId) ? "" : договорId.Replace('_', ' ');
                типЦен = string.IsNullOrEmpty(типЦен) ? "розничные" : типЦен.Replace('_', ' ').ToLower();
                картаId = string.IsNullOrEmpty(картаId) ? "" : картаId.Replace('_', ' ');
                var данные = (await _номенклатураRepository.НоменклатураЦенаКлиентаAsync(new List<string> { номенклатураId }, договорId, картаId, доставка, типДоставки, null)).FirstOrDefault();
                if (данные != null)
                {
                    data.Номенклатура = данные;
                    data.РозничнаяЦена = данные.Цена.Розничная;
                    data.ОсобаяЦена = данные.Цена.Особая;
                    data.СПрознЦена = данные.Цена.РозСП;
                    data.ОптоваяЦена = данные.Цена.Оптовая;
                    data.СПоптЦена = данные.Цена.ОптСП;
                    data.ПороговаяЦена = данные.Цена.Порог;
                    if (данные.Цена.Розничная > 0)
                        варианты.Add(1, "Розничная = " + данные.Цена.Розничная.ToString("C"));
                    if (данные.Цена.Особая > 0)
                        варианты.Add(2, "Особая = " + данные.Цена.Особая.ToString("C"));
                    if (данные.Цена.РозСП > 0)
                        варианты.Add(3, "СП розничная = " + данные.Цена.РозСП.ToString("C"));
                    if (типЦен != "розничные")
                    {
                        if (данные.Цена.Оптовая > 0)
                            варианты.Add(4, "Оптовая = " + данные.Цена.Оптовая.ToString("C"));
                        if (данные.Цена.ОптСП > 0)
                            варианты.Add(5, "СП оптовая = " + данные.Цена.ОптСП.ToString("C"));
                    }
                    if (данные.Цена.Порог > 0)
                        варианты.Add(6, "Порог цены = " + данные.Цена.Порог.ToString("C"));
                }
                варианты.Add(7, "Ручное изменение цены");
                варианты.Add(8, "Изменить на %");
            }
            else
            {
                data.Номенклатура = new Номенклатура { Id = "", Наименование = "не указана" };
            }
            data.Варианты = new SelectList(варианты, "Key", "Value");
            _номенклатураRepository.Dispose();
            return View(data);
        }
    }
}
