using SFML.Graphics;
using SFML.System;
using SFML.Audio;
using SFML.Window;


namespace Wolstencroft
{
    #region Maths
    //maths class for specific functions
    static class WMaths
    {
        //normalize a vector
        static public Vector2f Normalize(Vector2f input)
        {
            float fLength = MathF.Sqrt((input.X * input.X) + (input.Y * input.Y));

            return (input / fLength);
            
        }

        //float lerp 
        static public float Lerp(float a, float b, float T)
        {
            return (1-T)*a + b*T;
        }
        
        //vector lerp
        static public Vector2f Lerp(Vector2f a, Vector2f b, float T)
        {
            return (1-T)*a + b*T;
        }

        static public Vector2i V2FtoV2I(Vector2f input) => new Vector2i((int)input.X, (int)input.Y);

    }
    #endregion
    #region Component
    //polymorphic class
    class Component
    {
        //reference to the game
        protected Game game;
        //reference to Parent Entity
        protected Entity entity { get; private set; }
        //reference to the transform
        public Transform2D transform;

        //overridable functions 
        public virtual void OnUpdate() { }
        public virtual void OnStart() { }

        public Component()
        {
            game = Game.Instance;
        }

        public void SetEntityParent(ref Entity entity)
        {
            this.entity = entity;
            transform = entity.transform;
        }

    }

    //to render on screen 
    class Renderable : Component
    {
        public RectangleShape Body = new RectangleShape();


        public Renderable()
        {
        }

        public override void OnUpdate()
        {
            Body.Position = entity.transform.position;
            Body.Rotation = entity.transform.fRotation;
            Body.Size = entity.transform.size;
            Game.Instance.Draw(Body);
        }
    }

    //handle the transform of an object
    class Transform2D : Component
    {
        public Vector2f position = new Vector2f(0, 0);
        public Vector2f size = new Vector2f(0, 0);
        public float fRotation = 0f;
    }
    
    class Collider2D : Component
    {
        public FloatRect Collider;
        bool bIsColiding = false;

        public Action<Collider2D> OnCollisionEvent;
        public Action<Collider2D> OnExitCollisionEvent;
        public Collider2D()
        {
        }

        public override void OnUpdate()
        {
            Collider = new FloatRect(transform.position, (transform.size));
        }
        public bool IsColiding(Collider2D collider)
        {
            Game.Log(bIsColiding && Collider.Intersects(collider.Collider));
            if (!bIsColiding &&  Collider.Intersects(collider.Collider) == true)
            {
                OnCollisionEvent?.Invoke(this);
                bIsColiding = true;
            }
            else if(bIsColiding && Collider.Intersects(collider.Collider) == false)
            {
                OnExitCollisionEvent?.Invoke(this);
                bIsColiding = false;
            }
            return bIsColiding;

        }

    }    class ColliderManager : Component
    {
        List<Collider2D> colliders = new List<Collider2D>();

        public override void OnStart()
        {


        }

        public void AddCollider(Entity entity)
        {
            Collider2D col = entity.GetComponent<Collider2D>();
            if (col != null)
            {
                colliders.Add(col);
                Game.Log("Collision added");

            }
        }

        public void AddCollider(Collider2D col)
        {
            colliders.Add(col);
        }

        public void CheckAllForCollision()
        {
            foreach(Collider2D col in colliders)
            {
                foreach (Collider2D colx in colliders)
                {
                    //check if current collision is the same as comparing
                    if (colx == col) continue;

                    bool bProduct = col.IsColiding(colx);
                    
                }
            }
        }

    }
    class EntityManager : Component
    {
        public List<Entity> Entitys = new List<Entity>();
        ColliderManager colliderManager = new ColliderManager();

        public void HandleEntityUpdates()
        {
            HandlePhysicsUpdates();

            for (int i = 0; i < Entitys.Count; i++)
            {
                Entitys[i].RunUpdates();
            }

        }

        public void HandlePhysicsUpdates()
        {
            colliderManager.CheckAllForCollision();
        }

        public Entity AddEntity(Entity entity)
        {
            Game.Log($"Added: {(entity.Name)}");
            entity.RunStarts();

            //attempt to add a collider to the manager
            colliderManager.AddCollider(entity);

            Entitys.Add(entity);

            return entity;
        }

        public void Destroy(Entity entity)
        {
            Game.Log($"Destroying: {entity.Name}");

            for (int i = 0; i < Entitys.Count; i++)
            {
                if (entity == Entitys[i])
                {
                    Entitys.RemoveAt(i);
                    GC.Collect();
                    GC.SuppressFinalize(this);

                }
            }
        }
    }
    #endregion
    #region Entity
    class Entity
    {
        public List<Component> components = new List<Component>();
        public Transform2D transform;
        public string Name = "Entity";
        private Entity entityRef ;


        public Entity()
        {
            entityRef = this;
            //add transform2d by default
            transform = AddComponent<Transform2D>();
            if(this != null)
                Name = ToString();
        }

        //run all component starts
        public void RunStarts()
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].OnStart();
            }
        }
        public void RunUpdates()
        {
            //runs all component updates 
            foreach (var Comp in components)
            {
                Comp.OnUpdate();
            }
        }

        //add a component to the list
        public T AddComponent<T>() where T : Component, new()
        {
            T NewComp = new T();
            NewComp.SetEntityParent(ref entityRef);
            components.Add(NewComp);

            return NewComp;
        }

        //get a component from the list
        public T GetComponent<T>() where T : Component, new()
        {
            T NewComp = new T();

            foreach (var Comp in components)
            {
                if (NewComp.GetType() == Comp.GetType())
                {
                    return (T)Comp;
                }
            }

            return null;


        }


    };

    class EntityManagerOBJ : Entity
    {
        public EntityManager entityManager;



        public EntityManagerOBJ()
        {
            entityManager = AddComponent<EntityManager>();
        }
    }
   
   
    #endregion
    #region Game

    //main game logic
    class Game
    {
        public static Game Instance = null;

        public RenderWindow window;

        public uint iWidth = 400, iHeight = 400;

        public string sName = "Window";

        static Clock RunTimeClock = new Clock();

        public EntityManagerOBJ Entities;

        public Game()
        {
            if (Instance == null)
                Instance = this;
            window = new RenderWindow(new VideoMode(iWidth, iHeight), sName);
            Entities = new EntityManagerOBJ();
        }

        public void Draw(SFML.Graphics.Drawable drawable)
        {
            if (window != null && drawable != null)
                window.Draw(drawable);
        }


        public void Start()
        {
            RunTimeClock.Restart();
            window.Closed += HandleClose;
            window.KeyPressed += HandleKeyPress;

            HandleUpdate();
        }

        void HandleUpdate()
        {

            while (window.IsOpen)
            {
                window.DispatchEvents();
                window.Clear();

                //call all entity updates
                HandleEntityUpdates();

                window.Display();
            }
        }
        void HandleClose(object sender, EventArgs e)
        {
            window.Close();
            window.Closed -= HandleClose;
            window.KeyPressed -= HandleKeyPress;



        }
      

        void HandleKeyPress(object sender, SFML.Window.KeyEventArgs e)
        {
        }

        static public bool IsKeyPressed(SFML.Window.Keyboard.Key key)
        {
            return SFML.Window.Keyboard.IsKeyPressed(key);
        }

        static public void Log<T>(T Data)
        {
            Console.WriteLine($"[{RunTimeClock.ElapsedTime.AsSeconds()}][{Data}]");
        }
        
        static public Time GetTime()
        {
            return RunTimeClock.ElapsedTime;
        }

        public static Entity Instantiate(Entity entity)
        {
            if (Instance.Entities != null)
                Instance.Entities.entityManager.AddEntity(entity);

            return entity;
        }
        public static void Destroy(Entity entity)
        {
            Instance.Entities.entityManager.Destroy(entity);
        }
        static void HandleEntityUpdates()
        {
            Instance.Entities.entityManager.HandleEntityUpdates();
        }

    };
    #endregion
}

