﻿using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using MPTanks.Engine.Rendering;
using MPTanks.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Engine
{
    /// <summary>
    /// The base type all gameobjects derive from. You can create a completely custom type if you want, but
    /// reflective initialization and useful utilities will mostly not exist.
    /// </summary>
    public abstract class GameObject
    {
        #region Reflection helper
        //We cache the info for performance. Multiple calls only create one instance
        private string _cachedReflectionName;
        public string ReflectionName
        {
            get
            {
                if (_cachedReflectionName == null)
                    _cachedReflectionName = ((GameObjectAttribute[])(GetType()
                          .GetCustomAttributes(typeof(GameObjectAttribute), true)))[0]
                          .ReflectionTypeName;

                return _cachedReflectionName;
            }
        }
        private string _cachedDisplayName;
        public string DisplayName
        {
            get
            {
                if (_cachedDisplayName == null)
                    _cachedDisplayName = ((GameObjectAttribute[])(GetType()
                          .GetCustomAttributes(typeof(GameObjectAttribute), true)))[0]
                          .DisplayName;

                return _cachedDisplayName;
            }
        }
        private string _cachedDescription;
        public string Description
        {
            get
            {
                if (_cachedDescription == null)
                    _cachedDescription = ((GameObjectAttribute[])(GetType()
                          .GetCustomAttributes(typeof(GameObjectAttribute), true)))[0]
                          .Description;

                return _cachedDescription;
            }
        }

        private Module _cachedModule;
        /// <summary>
        /// The module that contains this object
        /// </summary>
        public Module ContainingModule
        {
            get
            {
                if (_cachedModule == null)
                    _cachedModule = ((GameObjectAttribute[])(GetType()
                          .GetCustomAttributes(typeof(GameObjectAttribute), true)))[0]
                          .Owner;
                return _cachedModule;
            }
        }
        #endregion
        #region Basic Properties
        public Color ColorMask { get; set; }
        public int ObjectId { get; private set; }
        public Body Body { get; private set; }
        public GameCore Game { get; private set; }
        public bool Alive { get; private set; }
        public bool IsDestructionCompleted { get; protected set; }
        protected Dictionary<string, Rendering.RenderableComponent> _components;
        public virtual IReadOnlyDictionary<string, Rendering.RenderableComponent>
            Components
        {
            get
            {
                if (_components == null)
                {
                    _components = new Dictionary<string, Rendering.RenderableComponent>();
                    //This is the first access so we call the component constructor
                    AddComponents(_components);
                }

                return _components;
            }
        }
        #endregion

        #region Wrappers for Farseer Body
        //Since farseer is at a much smaller scale, we normalize to world space
        public Vector2 Position
        {
            get
            {
                if (_hasBeenCreated)
                    return Body.Position / Game.Settings.PhysicsScale;
                else
                    return _startPosition;
            }
            set
            {
                if (_hasBeenCreated)
                    Body.Position = value * Game.Settings.PhysicsScale;
                else
                    _startPosition = value;
            }
        }

        /// <summary>
        /// The rotation of the physics object in radians
        /// </summary>
        public float Rotation
        {
            get
            {
                if (_hasBeenCreated)
                    return Body.Rotation;
                else
                    return _startRotation;
            }
            set
            {
                if (_hasBeenCreated)
                    Body.Rotation = value;
                else
                    _startRotation = value;
            }
        }

        public Vector2 LinearVelocity
        {
            get
            {
                if (_hasBeenCreated)
                    return Body.LinearVelocity / Game.Settings.PhysicsScale;
                else
                    return _startVelocity;
            }
            set
            {
                if (_hasBeenCreated)
                    Body.LinearVelocity = value * Game.Settings.PhysicsScale;
                else
                    _startVelocity = value;
            }
        }

        public float AngularVelocity
        {
            get
            {
                if (_hasBeenCreated)
                    return Body.AngularVelocity;
                else
                    return _startAngularVelocity;
            }
            set
            {
                if (_hasBeenCreated)
                    Body.AngularVelocity = value;
                else
                    _startAngularVelocity = value;
            }
        }
        private Vector2 _size;
        public Vector2 Size
        {
            get { return _size; }
            protected set
            {
                if (_hasBeenCreated)
                {
                    //Build new shape
                    var vertices = new FarseerPhysics.Common.Vertices(new[] {
                        new Vector2(-value.X / 2, -value.Y /2),
                        new Vector2(value.X / 2, -value.Y /2),
                        new Vector2(value.X / 2, value.Y /2),
                        new Vector2(-value.X / 2, value.Y /2)
                    });
                    var rect = new FarseerPhysics.Collision.Shapes.PolygonShape(
                        vertices, Body.FixtureList[0].Shape.Density);

                    //Destroy current fixture
                    Body.DestroyFixture(Body.FixtureList[0]);
                    //And add the new one
                    Body.CreateFixture(rect, this);
                }
                //And set the internal note
                _size = value;
            }
        }

        #endregion
        #region Wrappers for Farseer Settings
        public bool IsStatic
        {
            get
            {
                if (_hasBeenCreated)
                    return Body.IsStatic;
                else
                    return _startIsStatic;
            }
            set
            {
                if (_hasBeenCreated)
                    Body.IsStatic = value;
                else
                    _startIsStatic = value;
            }
        }
        private bool _isSensor;
        public bool IsSensor
        {
            get
            {
                if (_hasBeenCreated)
                    return _isSensor;
                else
                    return _startIsSensor;
            }
            set
            {
                if (_hasBeenCreated)
                {
                    Body.IsSensor = value;
                    _isSensor = value;
                }
                else
                    _startIsSensor = value;
            }
        }
        #endregion

        #region Places to store settings pre-initialization
        private Vector2 _startPosition;
        private float _startDensity;
        private float _startBounciness;
        private float _startRotation;
        private Vector2 _startVelocity;
        private float _startAngularVelocity;
        private bool _startIsStatic;
        private bool _startIsSensor;
        #endregion
        public GameObject(GameCore game, bool authorized, float density = 1, float bounciness = 0.1f, Vector2 position = default(Vector2), float rotation = 0, int id = -1)
        {
            if (id == -1)
                ObjectId = game.NextObjectId;
            else
                ObjectId = id;
            Game = game;

            ColorMask = Color.White;
            _startPosition = position;
            _startRotation = rotation;
            _startDensity = density;
            _startBounciness = bounciness;

            if (!game.Authoritative && !authorized)
                game.Logger.Error("Object Created without authorization. Type: " + this.GetType().ToString() + ", ID: " + ObjectId);
        }

        private bool _hasBeenCreated;
        internal void Create()
        {
            if (_hasBeenCreated)
            {
                Game.Logger.Error("Multiple calls to Create()");
            }
            _hasBeenCreated = true;
            //Create the body in physics space, which is smaller than world space, which is smaller than render space
            Body = BodyFactory.CreateRectangle(Game.World, Size.X * Game.Settings.PhysicsScale,
                 Size.Y * Game.Settings.PhysicsScale, _startDensity, Vector2.Zero, _startRotation,
                 BodyType.Dynamic, this);
            Body.Restitution = _startBounciness;
            Body.OnCollision += Body_OnCollision;
            //And initialize the object
            Alive = true;
            Position = _startPosition;
            LinearVelocity = _startVelocity;
            AngularVelocity = _startAngularVelocity;
            IsSensor = _startIsSensor;
            IsStatic = _startIsStatic;

            //Call the event
            OnCreated(this, this);

            //And call the internal function
            CreateInternal();
        }

        #region Component Creation
        /// <summary>
        /// Called when the object is supposed to add the rendering components. Usually 
        /// on the first access but this is not guaranteed.
        /// Please note: the components are already loaded from the components file beforehand,
        /// so you only need this if you're programatically generating extra ones.
        /// NOTE: DO NOT USE THIS UNLESS YOU NEED TO. 
        /// </summary>
        protected virtual void AddComponents(Dictionary<string, RenderableComponent> components)
        {
        }

        /// <summary>
        /// Loads the components from the specified asset and adds them to the internal dictionary.
        /// </summary>
        /// <param name="assetName"></param>
        protected void LoadComponentsFromFile(string assetName)
        {

        }

        /// <summary>
        /// Loads the components specified in the file specified in the attribute for this object's type
        /// </summary>
        private void LoadBaseComponents()
        {
            var componentFile = ((GameObjectAttribute[])(GetType()
                  .GetCustomAttributes(typeof(GameObjectAttribute), true)))[0]
                  .ComponentFile;

            LoadComponentsFromFile(ResolveAsset(componentFile));
        }
        #endregion
        protected virtual void CreateInternal()
        {

        }

        private bool Body_OnCollision(Fixture fixtureA,
            Fixture fixtureB, FarseerPhysics.Dynamics.Contacts.Contact contact)
        {
            return CollideInternal((GameObject)fixtureB.Body.UserData, contact);
        }

        protected virtual bool CollideInternal(GameObject other,
            FarseerPhysics.Dynamics.Contacts.Contact contact)
        {
            return true;
        }

        /// <summary>
        /// Transforms a point that is relative to the top left of the physics object,
        /// based on the current body position and rotation.
        /// </summary>
        /// <param name="point">The point to transform</param>
        /// <param name="secondaryRotation">The secondary rotation of the object (provided by you).</param>
        /// <param name="transformOnlyBySecondaryRotation">Whether to calculate from both the object rotation and the provided rotation or just the provided rotation.</param>
        /// <returns></returns>
        protected Vector2 TransformPoint(Vector2 point, float secondaryRotation = 0, bool transformOnlyBySecondaryRotation = false)
        {
            float cos, sin;
            //Cache for performance
            if (transformOnlyBySecondaryRotation)
            {
                cos = (float)Math.Cos(secondaryRotation);
                sin = (float)-Math.Sin(secondaryRotation);
            }
            else
            {
                cos = (float)Math.Cos(Rotation + secondaryRotation);
                sin = (float)-Math.Sin(Rotation + secondaryRotation);
            }

            point = point - (Size / 2);
            var rotatedX = point.X * cos + point.Y * sin;
            var rotatedY = point.X * sin + point.Y * cos;
            var centered = new Vector2(rotatedX, rotatedY);
            var transformed = centered + Position;

            return transformed;
        }

        abstract public void Update(GameTime time);
        public virtual void UpdatePostPhysics(GameTime gameTime)
        {

        }

        private bool _hasBeenDeleted;
        internal bool Destroy(GameObject destructor = null)
        {
            if (_hasBeenDeleted)
            {
                Game.Logger.Error("Multiple calls to Destroy()");
            }
            Alive = false;
            _hasBeenDeleted = true;
            //Call the event
            _destroyedEventObj.Destroyed = this;
            _destroyedEventObj.Destroyer = destructor;
            OnDestroyed(this, _destroyedEventObj);

            var canDeleteRightAway = DestroyInternal(destructor);
            if (!Body.IsDisposed && canDeleteRightAway == false)
                Body.Dispose(); //Kill the physics body if allowed to delete

            return canDeleteRightAway;
        }
        /// <summary>
        /// Does object destruction logic. Return true if you would like to defer destruction until IsDestructionCompleted = true.
        /// Return false to delete right away
        /// </summary>
        /// <param name="destructor"></param>
        /// <returns></returns>
        protected virtual bool DestroyInternal(GameObject destructor = null)
        {
            return false;
        }

        /// <summary>
        /// Finalizes destruction logic
        /// </summary>
        internal void EndDestruction()
        {
            if (!Body.IsDisposed)
                Body.Dispose(); //Kill the physics body for sure
            Alive = false;
            OnRemovedFromGame(); //And call the destructor logic

            OnDestructionEnded(this, this);
        }

        protected virtual void OnRemovedFromGame()
        {

        }

        protected string ResolveAsset(string assetName)
        {
            if (GetType().IsSubclassOf(typeof(Tanks.Tank)))
                return SpriteSheetLookupHelper.ResolveAsset(ContainingModule.Name, assetName, ((Tanks.Tank)this).Player);
            return SpriteSheetLookupHelper.ResolveAsset(ContainingModule.Name, assetName);
        }

        protected string ResolveAsset(string modName, string assetName)
        {
            if (GetType().IsSubclassOf(typeof(Tanks.Tank)))
                return SpriteSheetLookupHelper.ResolveAsset(modName, assetName, ((Tanks.Tank)this).Player);
            return SpriteSheetLookupHelper.ResolveAsset(modName, assetName);
        }

        #region Events
        public event EventHandler<GameObject> OnCreated = delegate { };
        private Core.Events.Types.GameObjects.DestroyedEventArgs _destroyedEventObj =
            new Core.Events.Types.GameObjects.DestroyedEventArgs();
        public event EventHandler<Core.Events.Types.GameObjects.DestroyedEventArgs> OnDestroyed = delegate { };
        public event EventHandler<GameObject> OnDestructionEnded = delegate { };

        private Core.Events.Types.GameObjects.StateChangedEventArgs _stateArgs =
            new Core.Events.Types.GameObjects.StateChangedEventArgs();
        public event EventHandler<Core.Events.Types.GameObjects.StateChangedEventArgs> OnStateChanged = delegate { };

        protected void RaiseStateChangeEvent(byte[] newStateData)
        {
            if (OnStateChanged == null || !Game.Authoritative || newStateData == null || newStateData.Length == 0)
                return;

            _stateArgs.Object = this;
            _stateArgs.State = newStateData;
            OnStateChanged(this, _stateArgs);
        }

        const int JSONSerializedMagicNumber = unchecked((int)0xFA571337); //FAST LEET
        byte[] JSONSerializedMagicBytes = BitConverter.GetBytes(JSONSerializedMagicNumber);

        /// <summary>
        /// Serializes the object to JSON before sending it.
        /// </summary>
        /// <param name="obj"></param>
        protected void RaiseStateChangeEvent(object obj)
        {
            var serialized = SerializeStateChangeObject(obj);
            var count = Encoding.UTF8.GetByteCount(serialized);
            var array = new byte[count + 4];
            Array.Copy(Encoding.UTF8.GetBytes(serialized), 0, array, 4, count);
            Array.Copy(JSONSerializedMagicBytes, array, 4);
            RaiseStateChangeEvent(array);
        }

        protected string SerializeStateChangeObject(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public void ReceiveStateData(byte[] stateData)
        {
            if (BitConverter.ToInt32(stateData,0) == JSONSerializedMagicNumber)
            {
                //Try to deserialize
                var obj = DeserializeStateChangeObject(Encoding.UTF8.GetString(stateData, 4, stateData.Length - 4));
                ReceiveStateDataInternal(obj);
            }
            else
            {
                ReceiveStateDataInternal(stateData);
            }
        }

        protected object DeserializeStateChangeObject(string obj)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(obj);
        }
        protected T DeserializeStateChangeObject<T>(string obj)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(obj);
        }

        protected virtual void ReceiveStateDataInternal(byte[] stateData)
        {

        }

        protected virtual void ReceiveStateDataInternal(dynamic obj)
        {

        }

        #endregion
    }
}