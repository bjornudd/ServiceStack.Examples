using System;
using ServiceStack.Redis;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.ServiceHost;

//The entire C# source code for the ServiceStack + Redis TODO REST backend. There is no other .cs :)
namespace Backbone.Todos
{
	//Register REST Paths
	[RestService("/todos")]
	[RestService("/todos/{Id}")]
	public class Todo //REST Resource DTO
	{
		public long Id { get; set; }
		public string Content { get; set; }
		public int Order { get; set; }
		public bool Done { get; set; }
	}

	//Todo REST Service implementation
	public class TodoService : RestServiceBase<Todo>
	{
		public IRedisClientsManager RedisManager { get; set; }  //Injected by IOC

		public override object OnGet(Todo request)
		{
			//return all todos
			if (request.Id == default(long))
				return RedisManager.ExecAs<Todo>(r => r.GetAll());

			//return single todo
			return RedisManager.ExecAs<Todo>(r => r.GetById(request.Id));
		}

		//Handles creaing a new and updating existing todo
		public override object OnPost(Todo todo)
		{
			RedisManager.ExecAs<Todo>(r => {
				//Get next id for new todo
				if (todo.Id == default(long)) todo.Id = r.GetNextSequence();
				r.Store(todo);
			});
			return todo;
		}

		public override object OnDelete(Todo request)
		{
			RedisManager.ExecAs<Todo>(r => r.DeleteById(request.Id));
			return null;
		}
	}

	//Configure ServiceStack.NET web service host
	public class AppHost : AppHostBase
	{
		//Tell ServiceStack the name and where to find your web services
		public AppHost() : base("Backbone.js TODO", typeof(TodoService).Assembly) { }

		public override void Configure(Funq.Container container)
		{
			//Register Redis factory in Funq IOC
			container.Register<IRedisClientsManager>(new BasicRedisClientManager("localhost:6379"));
		}
	}

	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			new AppHost().Init(); //Start ServiceStack App
		}
	}
}