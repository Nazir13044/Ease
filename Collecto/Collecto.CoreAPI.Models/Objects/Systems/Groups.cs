using Collecto.CoreAPI.Models.Objects.Setups;

namespace Collecto.CoreAPI.Models.Objects.Systems
{
    public class PermissionBase
    {
        public string ModuleId { get; set; }
        public bool AllowSelect { get; set; }
        public bool AllowAdd { get; set; }
        public bool AllowEdit { get; set; }
        public bool AllowDelete { get; set; }
    }

    public class GroupPermission : PermissionBase
    {
        public GroupPermission()
        {
            Children = new List<GroupPermission>();
        }
        public string ModuleName { get; set; }
        public string Icon { get; set; }
        public string RouteUrl { get; set; }
        public string ParentId { get; set; }
        public bool Visible { get; set; }
        public List<GroupPermission> Children { get; set; }
        public bool HasChildren => Children != null && Children.Count > 0;
        public MenuItem Copy()
        {
            return new MenuItem { ModuleId = ModuleId, Label = ModuleName, Icon = Icon, RouterLink = RouteUrl };
        }
    }

    public class Menu : List<GroupPermission>
    {
        private readonly Stack<string> _keyStack;
        public Menu()
        {
            base.Clear();
            _keyStack = new Stack<string>();
        }

        public void Add(Menu menu)
        {
            foreach (GroupPermission gp in menu)
                Add(gp);
        }

        private string GetCurrentParent()
        {
            if (_keyStack.Count > 0)
                return _keyStack.Peek();
            else
                return null;
        }

        private void PushParent(string key)
        {
            _keyStack.Push(key);
        }

        private void PopParent()
        {
            _keyStack.Pop();
        }

        private GroupPermission GetItem(string moduleId)
        {
            return this.FirstOrDefault(gp => gp.ModuleId == moduleId);
        }

        private bool Exists(string moduleId)
        {
            return GetItem(moduleId) != null;
        }

        public void BeginGroup(string moduleId, string moduleName, string icon)
        {
            MenuItem(moduleId: moduleId, moduleName: moduleName, icon: icon, routeUrl: string.Empty, visible: true, allowSelect: false, allowAdd: false, allowEdit: false, allowDelete: false);
            PushParent(moduleId);
        }

        public void BeginGroup(string moduleId, string moduleName, bool visible)
        {
            MenuItem(moduleId: moduleId, moduleName: moduleName, icon: string.Empty, routeUrl: string.Empty, visible: visible, allowSelect: false, allowAdd: false, allowEdit: false, allowDelete: false);
            PushParent(moduleId);
        }

        public void MenuItem(string moduleId, string moduleName, bool visible)
        {
            MenuItem(moduleId: moduleId, moduleName: moduleName, icon: string.Empty, routeUrl: string.Empty, visible: visible, allowSelect: false, allowAdd: false, allowEdit: false, allowDelete: false);
        }

        public void MenuItem(string moduleId, string moduleName, string routeUrl, bool visible)
        {
            MenuItem(moduleId: moduleId, moduleName: moduleName, icon: string.Empty, routeUrl: routeUrl, visible: visible, allowSelect: false, allowAdd: false, allowEdit: false, allowDelete: false);
        }

        public void MenuItem(string moduleId, string moduleName, string icon, string routeUrl, bool visible)
        {
            MenuItem(moduleId: moduleId, moduleName: moduleName, icon: icon, routeUrl: routeUrl, visible: visible, allowSelect: false, allowAdd: false, allowEdit: false, allowDelete: false);
        }

        public void MenuItem(string moduleId, string moduleName, string icon, string routeUrl, bool visible, bool allowSelect, bool allowAdd, bool allowEdit, bool allowDelete)
        {
            if (Exists(moduleId) == false)
            {
                string parentKey = this.GetCurrentParent();
                if (string.IsNullOrEmpty(parentKey) == false)
                {
                    GroupPermission parent = GetItem(parentKey);
                    if (parent != null && parent.Visible == false)
                        visible = false;
                }

                GroupPermission item = new()
                {
                    Icon = icon,
                    ModuleId = moduleId,
                    ModuleName = moduleName,
                    RouteUrl = routeUrl,
                    ParentId = parentKey,
                    Visible = visible,
                    AllowSelect = allowSelect,
                    AllowAdd = allowAdd,
                    AllowEdit = allowEdit,
                    AllowDelete = allowDelete,
                };

                Add(item);
            }
        }

        public void EndGroup()
        {
            PopParent();
        }
    }

    public class MenuItem
    {
        public string ModuleId { get; set; }
        public string Label { get; set; }
        public string RouterLink { get; set; }
        public List<MenuItem> Items { get; set; }
        public string Icon { get; set; }
    }

    public class GroupBase : BaseObject
    {
        public int GroupId { get; set; }
        public UserTypeEnum UserType { get; set; }
    }

    public class GroupSearch : GroupBase
    {
        public string UserTypeDetail { get { return UserType.ToString(); } }
    }
}
