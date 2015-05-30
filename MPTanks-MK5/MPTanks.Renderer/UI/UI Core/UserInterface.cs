﻿using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Mvvm;
using EmptyKeys.UserInterface.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MPTanks.Rendering.UI.Binders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Rendering.UI
{
    public class UserInterface
    {
        public UserInterfacePage UIPage
        {
            get
            {
                return _page;
            }
            set
            {
                _page = value;
                _page.UserInterface = this;
                _page.Page.Resize(currentWidth, currentHeight);
                FontManager.Instance.LoadFonts(_content);
                ImageManager.Instance.LoadImages(_content);
                SoundManager.Instance.LoadSounds(_content);
            }
        }

        public dynamic ActiveBinder
        {
            get
            {
                return _page.Binder;
            }
        }

        public TimeSpan PageTransitionTime { get; set; }

        private ContentManager _content;
        private UserInterfacePage _page;
        private GraphicsDevice _graphics;
        private EmptyKeys.UserInterface.Engine _engine;
        private List<MessageBox> _messageBoxes = new List<MessageBox>();
        private UserInterfacePage _activeMessageBox;

        private UserInterfacePage _activePage
        {
            get
            {
                if (_activeMessageBox != null)
                    return _activeMessageBox;
                else return _page;
            }
        }

        public UserInterface(ContentManager contentManager, GraphicsDevice gd)
        {
            _engine = new MonoGameEngine(gd, gd.Viewport.Width, gd.Viewport.Height);
            _content = new ContentManager(contentManager.ServiceProvider, "assets/ui/imgs");
            SpriteFont font = _content.Load<SpriteFont>("Segoe_UI_12_Regular");
            FontManager.DefaultFont = EmptyKeys.UserInterface.Engine.Instance.Renderer.CreateFont(font);
            _graphics = gd;
            PageTransitionTime = TimeSpan.FromMilliseconds(500);
        }

        public UserInterfacePage SetPage(string page)
        {
            UIPage = new UserInterfacePage(page);
            return UIPage;
        }

        private int currentWidth;
        private int currentHeight;
        public void Update(GameTime gameTime)
        {
            if (currentWidth != _graphics.Viewport.Width ||
                currentHeight != _graphics.Viewport.Height)
            {
                currentWidth = _graphics.Viewport.Width;
                currentHeight = _graphics.Viewport.Height;
                if (_page != null)
                    _page.Page.Resize(_graphics.Viewport.Width, _graphics.Viewport.Height);
                if (_activeMessageBox != null)
                    _activeMessageBox.Page.Resize(_graphics.Viewport.Width, _graphics.Viewport.Height);

                _engine = new MonoGameEngine(_graphics, _graphics.Viewport.Width, _graphics.Viewport.Height);
            }

            if (_activePage != null)
                _activePage.Update(gameTime);
        }
        public void Draw(GameTime gameTime)
        {
            _activePage.Draw(gameTime);
        }

        private float GetPercentageStepForTransition(GameTime gameTime)
        {
            return (float)(gameTime.ElapsedGameTime.TotalMilliseconds / PageTransitionTime.TotalMilliseconds);
        }

        #region Message Boxes
        public enum MessageBoxType
        {
            OKMessageBox,
            WarningMessageBox,
            ErrorMessageBox
        }
        public enum MessageBoxButtons
        {
            Ok,
            OkCancel,
            YesNo,
            YesNoCancel,
            OkNo,
            OkNoCancel
        }

        public enum MessageBoxResult
        {
            Ok,
            Cancel,
            Yes,
            No
        }

        public void ShowMessageBox(string header, string content, MessageBoxType type,
            MessageBoxButtons buttons, Action<MessageBoxResult> callback)
        {

            _messageBoxes.Add(new MessageBox()
            {
                Header = header,
                Content = content,
                Type = type,
                Buttons = buttons,
                Result = callback
            });

            if (_messageBoxes.Count == 1)
            {
                CreateMessageBox(_messageBoxes[0]);
                _messageBoxes.Clear();
            }
        }

        private void CreateMessageBox(MessageBox specification)
        {
            var page = new UserInterfacePage(specification.Type.ToString());
            page.Page.Resize(currentWidth, currentHeight);
            page.Page.Opacity = 1;

            FontManager.Instance.LoadFonts(_content);
            ImageManager.Instance.LoadImages(_content);
            SoundManager.Instance.LoadSounds(_content);

            page.Binder.Header = specification.Header;
            page.Binder.Content = specification.Content;
            page.Binder.Buttons = specification.Buttons;
            page.Binder.OkCommand = new RelayCommand((obj) =>
            {
                CloseMessageBox();
                specification.Result(MessageBoxResult.Ok);
            });
            page.Binder.CancelCommand = new RelayCommand((obj) =>
            {
                CloseMessageBox();
                specification.Result(MessageBoxResult.Cancel);
            });
            page.Binder.YesCommand = new RelayCommand((obj) =>
            {
                CloseMessageBox();
                specification.Result(MessageBoxResult.Yes);
            });
            page.Binder.NoCommand = new RelayCommand((obj) =>
            {
                CloseMessageBox();
                specification.Result(MessageBoxResult.No);
            });

            _activeMessageBox = page;
        }

        private void CloseMessageBox()
        {
            _activeMessageBox = null;
            UIPage = _page;
            _page.Page.DataContext = _page.Binder;
            if (_messageBoxes.Count > 0)
            {
                CreateMessageBox(_messageBoxes[0]);
                _messageBoxes.RemoveAt(0);
            }
            else CrappyReflectionHackToFixBrokenBindersBreakingThePagesInEmptyKeysBecauseFckLogic();
        }

        private void CrappyReflectionHackToFixBrokenBindersBreakingThePagesInEmptyKeysBecauseFckLogic()
        {
            var newPage = new UserInterfacePage(UIPage.Page.GetType().Name);
            ViewModelBase binder = _page.Binder;
            ViewModelBase newBinder = newPage.Binder;

            foreach (var property in binder.GetType().GetProperties(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public
                ))
            {
                if (property.CanWrite && property.CanRead)
                {
                    var value = property.GetValue(binder);
                    property.SetValue(newBinder,value );
                }
            }

            UIPage = newPage;
        }

        private class MessageBox
        {
            public string Header { get; set; }
            public string Content { get; set; }
            public MessageBoxType Type { get; set; }
            public MessageBoxButtons Buttons { get; set; }
            public Action<MessageBoxResult> Result { get; set; }
        }
        #endregion
    }
}
