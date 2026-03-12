using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Common
{
    public interface ISoftDeletable
    {
        bool IsActive { get; }
        void Deactivate();
    }
}
