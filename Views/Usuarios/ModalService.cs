using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Views.Usuarios
{
    public interface IModalService
    {
        void MostrarCargoModal(CatalogosManagementViewModel vm);
    }

    public class ModalService : IModalService
    {
        public void MostrarCargoModal(CatalogosManagementViewModel vm)
        {
            var window = new CargoModalWindow(vm);
            window.ShowDialog();
        }
    }
}
