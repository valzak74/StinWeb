using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace StinWeb.Models.DataManager.Справочники
{
    public class User
    {
        public int RowId { get; set; }
        public string Id { get; set; }
        [Required(ErrorMessage = "Не указано Имя пользователя")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Не указан Пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public Склад ОсновнойСклад { get; set; }
        public ПодСклад ОсновнойПодСклад { get; set; }
        public Фирма ОсновнаяФирма { get; set; }
        public Касса ОсновнаяКасса { get; set; }
    }
}
