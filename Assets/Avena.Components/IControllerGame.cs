namespace Avena.Components
{
	public interface IControllerGame
	{
		public void Enable(CompApp composition);

		public void Disable(CompApp composition);

		public void Update(CompApp composition);
	}
}