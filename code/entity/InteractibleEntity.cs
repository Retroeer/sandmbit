using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.entity
{
	public class InteractibleEntity : ModelEntity
	{
		public InteractibleEntity(string modelName) : base(modelName) { }

		public virtual bool CanInteract(Entity entity) { return true; }

		public virtual void OnInteract(Entity entity) {}
	}
}
