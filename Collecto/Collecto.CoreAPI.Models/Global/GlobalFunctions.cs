using Collecto.CoreAPI.Models.Objects.Systems;
using System.Data;

namespace Collecto.CoreAPI.Models.Global
{
    public class FormatItems : List<FormatItem>
    {

    }
    public class FormatItem
    {
        public FormatItem()
        {

        }

        public string Format { get; set; }
        public string GetFormat()
        {
            if (this.Occurrence > 0)
                return new string(this.Format[0], this.Occurrence);

            return string.Empty;
        }

        public int Occurrence { get; set; }
        public string OccurrenceFmtValue
        {
            get { return "{" + this.Occurrence + "}"; }
        }

        #region Functions

        public static List<FormatItems> MakeMultiFormatList(string formats)
        {
            List<FormatItems> list = [];

            string[] tokens = formats.Split('|');
            foreach (var format in tokens)
            {
                FormatItems fmtItems = [];
                List<FormatItem> tmpItems = MakeList(format);
                fmtItems.AddRange(tmpItems);
                list.Add(fmtItems);
            }

            return list;
        }

        public static List<FormatItem> MakeList(string format)
        {
            List<FormatItem> list = [];

            for (int idx = 0; idx < format.Length; idx++)
            {
                FormatItem item = new();
                if (list.Count > 0)
                {
                    item = list[^1];
                    if (item.Format.Trim() == format[idx].ToString().Trim())
                        item.Occurrence += 1;
                    else
                        item = new FormatItem();
                }
                if (item.Occurrence <= 0)
                {
                    item.Format = format[idx].ToString();
                    item.Occurrence = 1;
                    list.Add(item);
                }
            }

            return list;
        }

        public static FormatItems MakeConsecutiveOccurrenceList(string value, int maxAllowableSameDigits)
        {
            FormatItems list = [];
            if (string.IsNullOrEmpty(value))
                return list;

            for (int idx = 0; idx < value.Length; idx++)
            {
                string chr = value[idx].ToString();
                FormatItem item = new();
                if (list.Count > 0)
                {
                    item = list[^1];
                    if (item.Format.Trim() == chr.ToString().Trim())
                        item.Occurrence += 1;
                    else
                        item = new FormatItem();
                }

                if (item.Occurrence <= 0)
                {
                    item.Format = chr.ToString();
                    item.Occurrence = 1;
                    list.Add(item);
                }
            }

            IEnumerable<FormatItem> tmpList = list.Where(x => x.Occurrence > maxAllowableSameDigits);
            list = new FormatItems();
            if (tmpList.Any())
            {
                list.AddRange(tmpList);
            }

            return list;
        }

        #endregion
    }

    public static class GlobalFunctions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allowReverseOrder"></param>
        /// <param name="isExactFormatLength"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GenRegExpression(bool allowReverseOrder, bool isExactFormatLength, string format)
        {
            try
            {
                if (string.IsNullOrEmpty(format))
                    return string.Empty;

                string re = string.Empty;
                List<FormatItems> fmtItems = FormatItem.MakeMultiFormatList(format);

                //Generate expression
                foreach (var list in fmtItems)
                {
                    if (re.Length > 0)
                        re += "|";

                    string exp1 = string.Empty, exp2 = string.Empty;
                    foreach (var item in list)
                    {
                        string exp = string.Format("{0}{1}", (item.Format == "0" ? @"\d" : "[A-Z]"), item.OccurrenceFmtValue);
                        exp1 += exp;
                        exp2 = exp + exp2;
                    }
                    //Final expression
                    if (allowReverseOrder)
                    {
                        if (isExactFormatLength)
                            re += string.Format("^{0}$|^{1}$", exp1, exp2);
                        else
                            re += string.Format("^{0}|^{1}", exp1, exp2);
                    }
                    else
                    {
                        if (isExactFormatLength)
                            re += string.Format("^{0}$", exp1);
                        else
                            re += string.Format("^{0}", exp1);
                    }
                }
                return re;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string LineData(this DataRow row, string delimiter)
        {
            return string.Join(delimiter, row.ItemArray);
        }

        #region Menu Related Functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="srcData"></param>
        private static void MakeHierarchy(GroupPermission parent, List<GroupPermission> srcData)
        {
            try
            {
                IEnumerable<GroupPermission> children = srcData.Where(x => x.ParentId == parent.ModuleId);
                parent.Children.AddRange(children);
                foreach (GroupPermission child in parent.Children)
                {
                    MakeHierarchy(parent: child, srcData: srcData);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Menu HierarchicalMenu()
        {
            Menu menu = [];

            Menu flatMenu = BuildMenu();
            IEnumerable<GroupPermission> data = flatMenu.Where(x => (string.IsNullOrEmpty(x.ParentId) || string.IsNullOrWhiteSpace(x.ParentId)));
            menu.AddRange(data);

            foreach (GroupPermission parent in menu)
            {
                MakeHierarchy(parent: parent, srcData: flatMenu);
            }

            return menu;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Menu BuildMenu()
        {
            Menu menu = [];

            menu.BeginGroup("SDS.1", "Setup", "fa fa-wrench");
            menu.MenuItem("SDS.1.1", "Company Information", "fa fa-cog", "/thissystem", true);
            menu.MenuItem("SDS.1.1A", "SUBC/Subcompany", "fa fa-cogs", "/subsystems", true);

            return menu;
        }

        #endregion
        /// <summary>
        /// 
        /// </summary>
        public static class SystemDefinedTransactionTypeId
        {
            public const int Initial = 5;
        }
    }
}
