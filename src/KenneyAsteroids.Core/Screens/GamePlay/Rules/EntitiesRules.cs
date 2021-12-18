﻿using KenneyAsteroids.Core.Entities;
using KenneyAsteroids.Core.Events;
using KenneyAsteroids.Engine.Entities;
using KenneyAsteroids.Engine.Graphics;
using KenneyAsteroids.Engine.Rules;
using System.Numerics;

namespace KenneyAsteroids.Core.Screens.GamePlay.Rules
{
    public static class EntitiesRules
    {
        public static class WhenEntityIsCreated
        {
            public sealed class ThenAddItToTheWorld : IRule<EntityCreatedEvent>
            {
                private readonly IWorld _world;

                public ThenAddItToTheWorld(IWorld world)
                {
                    _world = world;
                }

                public bool ExecuteCondition(EntityCreatedEvent @event) => true;

                public void ExecuteAction(EntityCreatedEvent @event)
                {
                    _world.Add(@event.Entity);
                }
            }
        }
       
        public static class WhenAsteoidDestroyed
        {
            public sealed class ThenRemoveAsteroid : IRule<AsteroidDestroyedEvent>
            {
                private readonly IWorld _world;

                public ThenRemoveAsteroid(IWorld world)
                {
                    _world = world;
                }

                public bool ExecuteCondition(AsteroidDestroyedEvent @event) => true;

                public void ExecuteAction(AsteroidDestroyedEvent @event) => _world.Remove(@event.Asteroid);
            }
        }

        public static class WhenPlayersShipDestroyed
        {
            public sealed class ThenResetPlayersShip : IRule<ShipDestroyedEvent>
            {
                private readonly IViewport _viewport;

                public ThenResetPlayersShip(IViewport viewport)
                {
                    _viewport = viewport;
                }

                public bool ExecuteCondition(ShipDestroyedEvent @event) => true;

                public void ExecuteAction(ShipDestroyedEvent @event)
                {
                    @event.Ship.Position = new Vector2(_viewport.Width / 2, _viewport.Height / 2);
                    @event.Ship.Reset();
                }
            }
        }
    }
}
