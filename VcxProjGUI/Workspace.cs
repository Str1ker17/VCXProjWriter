using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VcxProjLib;

namespace VcxProjGUI
{
    /*
     * поскольку без нормального тз получается хз, попытаюсь описать что нужно:
     * - в программе в один момент времени открыт только один солюшен;
     * - программа умеет запоминать удалённые сервера с ssh в своём хранилище;
     * - можно сохранить недоделанный "проект" солюшена;
     * - можно видеть список всех файлов в солюшене, структурированный 
     */

    internal class Workspace {
        protected Solution sln;

        public Workspace() {

        }


    }
}
