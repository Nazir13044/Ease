namespace Collecto.CoreAPI.Models.Global
{
    public class MenuSettings
    {
        public List<MenuSettingItem> MenuItems { get; set; }

        public MenuSettingItem GetItem(string key, string value)
        {
            if (MenuItems == null || MenuItems.Count <= 0 || string.IsNullOrEmpty(key))
                return new MenuSettingItem { Key = key, Value = value };

            MenuSettingItem item = MenuItems.FirstOrDefault(x => x.Key == key);
            item ??= new MenuSettingItem { Key = key, Value = value };

            return item;
        }
    }

    public class MenuSettingItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
