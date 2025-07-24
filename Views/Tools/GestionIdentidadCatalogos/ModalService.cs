using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Views.Usuarios
{
    public interface IModalService
    {
        void MostrarCargoModal(CatalogosManagementViewModel vm);
        void MostrarTipoDocumentoModal(CatalogosManagementViewModel vm);
    }

    public class ModalService : IModalService
    {
        public void MostrarCargoModal(CatalogosManagementViewModel vm)
        {
            var window = new CargoModalWindow(vm);
            window.ShowDialog();
        }

        public void MostrarTipoDocumentoModal(CatalogosManagementViewModel vm)
        {
            var window = new TipoDocumentoModalWindow(vm);
            window.ShowDialog();
        }
    }
}
