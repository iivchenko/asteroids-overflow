﻿using KenneyAsteroids.Engine;
using KenneyAsteroids.Engine.Collisions;
using KenneyAsteroids.Engine.Entities;
using KenneyAsteroids.Engine.Graphics;
using KenneyAsteroids.Engine.Particles;
using KenneyAsteroids.Engine.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace KenneyAsteroids.Core.Entities
{
    public sealed class Ship : IEntity<Guid>, IUpdatable, IDrawable, IBody
    {
        private readonly IPainter _draw;
        private readonly IEventPublisher _publisher;
        private readonly Sprite _sprite;
        private readonly Sprite[] _debri;
        private readonly Weapon _weapon;
        private readonly float _maxSpeed;
        private readonly float _maxAcceleration;
        private readonly float _maxRotation;

        private Vector2 _velocity;
        private ShipAction _action;
        private IState _state;

        public Ship(
            IPainter draw,
            IEventPublisher publisher,
            Sprite sprite,
            Sprite[] debri,
            Weapon weapon,
            float maxSpeed,
            float maxAcceleration,
            float maxRotation)
        {
            _draw = draw;
            _publisher = publisher;
            _sprite = sprite;
            _debri = debri;
            _weapon = weapon;
            _maxSpeed = maxSpeed;
            _maxAcceleration = maxAcceleration;
            _maxRotation = maxRotation;

            _velocity = Vector2.Zero;
            _action = ShipAction.None;

            Id = Guid.NewGuid();
            Origin = new Vector2(_sprite.Width / 2.0f, _sprite.Height / 2.0f);
            Position = Vector2.Zero;
            Rotation = 0.0f;
            Scale = Vector2.One;
            Width = _sprite.Width;
            Height = _sprite.Height;
            Data = _sprite.ReadData();

            _state = new AliveState(this);
        }
        
        public Guid Id { get; }
        public IEnumerable<string> Tags => Enumerable.Empty<string>();
        public Vector2 Position { get; set; }
        public Vector2 Origin { get; set; }
        public Vector2 Scale { get; set; }
        public float Rotation { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Color[] Data { get; set; }
        public ShipState State 
        {
            get => _state switch
            {
                ResetState _ => ShipState.Reset,
                AliveState _ => ShipState.Alive,
                DestroyState _ => ShipState.Destroy,
                DeadState _ => ShipState.Dead,
                _ => throw new NotImplementedException()
            };
        }

        public void Apply(ShipAction action)
        {
            _action = action;
        }

        public void Reset()
        {
            _state = new ResetState(this);
        }

        public void Destroy()
        {
            _state = new DestroyState(this);
        }

        void IUpdatable.Update(float time)
        {
            _state.Update(time);
        }

        void IDrawable.Draw(float time)
        {
            _state.Draw(time);
        }

        private interface IState
        {
            void Update(float time);

            void Draw(float time);
        }

        private class AliveState : IState
        {
            public readonly Ship _ship;

            public AliveState(Ship ship)
            {
                _ship = ship;
            }

            public virtual void Draw(float time)
            {
                _ship
                    ._draw
                        .Draw(
                            _ship._sprite,
                            _ship.Position,
                            _ship.Origin,
                            _ship.Scale,
                            _ship.Rotation,
                            Colors.White);
            }

            public virtual void Update(float time)
            {
                _ship._weapon.Update(time);

                if (_ship._action.HasFlag(ShipAction.Left))
                    _ship.Rotation -= _ship._maxRotation * time;

                if (_ship._action.HasFlag(ShipAction.Right))
                    _ship.Rotation += _ship._maxRotation * time;

                if (_ship._action.HasFlag(ShipAction.Accelerate))
                {
                    var velocity = _ship._velocity + _ship.Rotation.ToDirection() * _ship._maxAcceleration;

                    _ship._velocity = velocity.Length() > _ship._maxSpeed ? Vector2.Normalize(velocity) * _ship._maxSpeed : velocity;
                }

                _ship.Position += _ship._velocity * time;

                if (_ship._action.HasFlag(ShipAction.Fire))
                    _ship._weapon.Fire(_ship.Position, _ship.Rotation);

                _ship._action = ShipAction.None;
            }
        }

        private sealed class DestroyState : IState
        {
            private readonly Ship _ship;
            private readonly ParticleEngine _particleEngine;

            public DestroyState(Ship ship)
            {
                _ship = ship;

                _particleEngine =
                    Particles
                        .CreateNew()
                        .WithInit(rand =>
                            Enumerable
                                .Range(0, rand.Next(5, 10))
                                .Select(_ =>
                                    new Particle
                                    {
                                        Angle = rand.Next(0, 360).AsRadians(),
                                        AngularVelocity = rand.Next(5, 100).AsRadians(),
                                        Color = Colors.White,
                                        Position = _ship.Position,
                                        Scale = Vector2.One,
                                        Sprite = _ship._debri[rand.Next(_ship._debri.Length)],
                                        TTL = 0.8f,
                                        Velocity = new Vector2(rand.Next(-100, 100), rand.Next(-100, 100))
                                    }))
                                .WithUpdate((rand, time, particle) =>
                                {
                                    particle.Position += particle.Velocity * time;
                                    particle.Angle += particle.AngularVelocity * time;
                                    particle.TTL -= time;
                                    particle.Color *= 0.99f;
                                })
                                .Build((int)DateTime.Now.Ticks, _ship._draw, time => _ship._state = new DeadState(_ship));
            }

            public void Draw(float time)
            {
                _particleEngine.Draw(time);
            }

            public void Update(float time)
            {
                _particleEngine.Update(time);
            }
        }

        private sealed class ResetState : AliveState
        {
            private float _ttl = 2.5f;

            public ResetState(Ship ship)
                : base (ship)
            {
                _ship._velocity = Vector2.Zero;
            }

            public override void Draw(float time)
            {
                var pulsate = MathF.Abs(MathF.Sin(_ttl * 15)) * 0.3f + 0.1f;

                _ship
                     ._draw
                         .Draw(
                             _ship._sprite,
                             _ship.Position,
                             _ship.Origin,
                             _ship.Scale,
                             _ship.Rotation,
                             Colors.White * pulsate);
            }

            public override void Update(float time)
            {
                base.Update(time);

                _ttl -= time;
                if (_ttl < 0)
                {
                    _ship._state = new AliveState(_ship);
                }
            }
        }

        private sealed class DeadState : IState
        {
            public DeadState(Ship ship) 
            {
                ship._publisher.Publish(new ShipDestroyedEvent(ship));
            }

            public void Draw(float time) { }

            public void Update(float time) { }
        }
    }

    [Flags]
    public enum ShipAction
    {
        None = 0b0000,
        Accelerate = 0b0001,
        Left = 0b0010,
        Right = 0b0100,
        Fire = 0b1000
    }

    public enum ShipState
    {
        Alive,
        Destroy,
        Reset,
        Dead
    }

    public sealed class ShipDestroyedEvent : IEvent
    {
        public ShipDestroyedEvent(Ship ship)
        {
            Id = Guid.NewGuid();
            Ship = ship;
        }

        public Guid Id { get; }

        public Ship Ship { get; }
    }
}
