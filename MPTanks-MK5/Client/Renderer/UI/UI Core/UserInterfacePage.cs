﻿
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Mvvm;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Renderer.UI
{
    public class UserInterfacePage
    {
        public UIRoot Page { get; private set; }
        public dynamic Binder { get; private set; }
        public UserInterface UserInterface { get; internal set; }

        public UserInterfacePage(string pageName)
        {
            //Generate an instance of the page
            Page = (UIRoot)Activator.CreateInstance(Type.GetType(nameof(EmptyKeys.UserInterface.Generated) + "." + pageName, true, true), 0, 0);
            Binder = Activator.CreateInstance(Type.GetType(nameof(Binders) + "." + pageName, true, true));
            if (Binder is BinderBase)
                Binder.Owner = this;

            Page.DataContext = Binder;
        }

        public virtual void Update(GameTime gameTime, bool isActiveWindow)
        {
            if (isActiveWindow)
                Page.UpdateInput(gameTime.ElapsedGameTime.TotalMilliseconds);
            Page.UpdateLayout(gameTime.ElapsedGameTime.TotalMilliseconds);
        }
        public virtual void Draw(GameTime gameTime)
        {
            Page.Draw(gameTime.ElapsedGameTime.TotalMilliseconds);
        }
    }
}
