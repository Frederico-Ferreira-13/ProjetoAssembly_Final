using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Common
{
    public interface IEntity
    {
        int GetId();
        void SetId(int id);
        bool GetIsActive();
    }
}
