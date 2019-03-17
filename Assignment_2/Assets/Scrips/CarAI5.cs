﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI5 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;
        public GameObject pathFinder;

        enum Formation { Drive, Attack, Back }

        public float maxVelocity;
        public float acceleration;
        private float timeStep;
        private int numberOfSteps;
        private float time;

        private float steerDirection;
        private float accelerationDirection;
        private float brake;
        private float handBrake;

        private bool crashed;
        private float crashTime;
        private float crashCheckTime;
        private float crashDirection;
        private Vector3 previousPosition;
        private ConfigurationSpace configurationSpace;
        public int currentPathIndex = 0;
        private readonly int distanceOffset = 3;
        private List<Vector3> finalPath;
        private int finalPathIndex;
        private readonly float EPSILON = 0.01f;
        private Formation formation = Formation.Drive;
        public bool inFormation = false;
        int backingTimeSteps = 0;
        int maxBackingTimeSteps = 100;

        public bool leader;
        public int carNumber;
        Vector3 offset = Vector3.zero;
        float angle;

        private void Start()
        {

            Time.timeScale = 1;
            maxVelocity = 10f;
            acceleration = 1f;

            timeStep = 0.05f;
            numberOfSteps = 5;
            time = 0;

            steerDirection = 0;
            accelerationDirection = 0;
            brake = 0;
            handBrake = 0;
            crashed = false;
            crashTime = 0;
            crashCheckTime = 0.5f;
            crashDirection = 0;
            previousPosition = Vector3.up;
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();

            InitializeCSpace();

            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");

            carNumber = 0;
            for (int i = 0; i < friends.Length; ++i)
            {
                if (friends[i].name == this.name)
                {
                    carNumber = i;
                    break;
                }
            }
            angle = 30;

            finalPathIndex = 0;
            finalPath = pathFinder.GetComponent<CreateGridCost>().path[finalPathIndex];

            Debug.Log(finalPath.Count);
        }

        private void Stop()
        {
            if (Math.Abs(m_Car.GetComponent<Rigidbody>().velocity.magnitude) > EPSILON)
            {
                m_Car.Move(0f, 0f, 0f, 1);
            }
        }

        private void Attack()
        {

        }

        private void FixedUpdate()
        {
            if (leader)
            {
                bool allInformation = true;
                foreach (GameObject friend in friends)
                {
                    if (!friend.GetComponent<CarAI5>().inFormation)
                    {
                        allInformation = false;
                        break;
                    }
                }

                if (allInformation)
                {
                    foreach (GameObject friend in friends)
                    {
                        friend.GetComponent<CarAI5>().formation = Formation.Attack;

                    }
                    inFormation = false;
                }
                if (Vector3.Distance(m_Car.transform.position, finalPath[currentPathIndex]) < 2f &&
                    currentPathIndex < finalPath.Count - 1)
                {
                    currentPathIndex++;
                }
                if (currentPathIndex >= finalPath.Count - 1)
                {
                    finalPathIndex++;
                    finalPath = pathFinder.GetComponent<CreateGridCost>().path[finalPathIndex];
                    currentPathIndex = 0;
                    /*
                    bool leaderSet = false;
                    foreach (GameObject friend in friends)
                    {
                        CarAI5 friendAI = friend.GetComponent<CarAI5>();
                        if (friendAI.carNumber != carNumber && !leaderSet)
                        {
                            friendAI.leader = true;
                            leader = false;
                            friendAI.finalPathIndex = finalPathIndex;
                            friendAI.finalPath = pathFinder.GetComponent<CreateGridCost>().path[finalPathIndex];
                            friendAI.currentPathIndex = 0;
                            leaderSet = true;
                        }
                        friendAI.formation = Formation.Back;
                        friendAI.inFormation = false;
                        
                    } */
                }


                if (currentPathIndex > finalPath.Count - 8 && !inFormation && formation == Formation.Drive)
                {
                    Stop();
                    inFormation = true;

                }

            }


            Vector3 followPoint = FindFollowPoint();

            if (formation == Formation.Back && !leader)
            {
                if (backingTimeSteps < maxBackingTimeSteps)
                {
                    m_Car.Move(0f, -1f, -1f, 0f);
                    backingTimeSteps++;
                } else if(Math.Abs(m_Car.GetComponent<Rigidbody>().velocity.magnitude) > EPSILON)
                {
                    Stop();
                } else
                {
                    backingTimeSteps = 0;

                    foreach (GameObject friend in friends)
                    {
                        CarAI5 friendAI = friend.GetComponent<CarAI5>();
                        friendAI.formation = Formation.Drive;
                        friendAI.inFormation = false;

                    }
                    return;
                    
                    
                }
               
                return;
            }

            if (followPoint == null)
            {
                return;
            } else if(followPoint == Vector3.zero)
            {
                if(formation == Formation.Drive) Stop();
                return;
            }


            if(!leader)
            {
                if (Vector3.Distance(m_Car.transform.position, followPoint) < 1.5f)
                {
                    Stop();
                    if (Math.Abs(m_Car.GetComponent<Rigidbody>().velocity.magnitude) < EPSILON) inFormation = true;
                    return;
                }
            }

            if (!crashed)
            {
                if (!leader || currentPathIndex < finalPath.Count - 1)
                {
                    time += Time.deltaTime;
                    if (time >= crashCheckTime && Vector3.Distance(m_Car.transform.position, terrain_manager.myInfo.start_pos) > 5)
                    {
                        time = 0;
                        if (Vector3.Distance(previousPosition, m_Car.transform.position) < 0.1f)
                        {
                            crashed = true;
                            if (Physics.BoxCast(
                                m_Car.transform.position,
                                new Vector3(configurationSpace.BoxSize.x / 2, configurationSpace.BoxSize.y / 2, 0.5f),
                                m_Car.transform.forward,
                                Quaternion.LookRotation(m_Car.transform.forward),
                                configurationSpace.BoxSize.z / 2
                            ))
                            {
                                crashDirection = -1;
                            }
                            else
                            {
                                crashDirection = 1;
                            }

                        }
                        else
                        {
                            previousPosition = m_Car.transform.position;
                        }
                    }
                }

                steerDirection = SteerInput(m_Car.transform.position, m_Car.transform.eulerAngles.y, followPoint);
                accelerationDirection = AccelerationInput(m_Car.transform.position, m_Car.transform.eulerAngles.y, followPoint);

                if (m_Car.CurrentSpeed >= maxVelocity)
                {
                    accelerationDirection = 0;
                }

                if (accelerationDirection < 0)
                {
                    if (formation == Formation.Attack)
                    {
                        foreach (GameObject friend in friends)
                        {
                            CarAI5 carAI = friend.GetComponent<CarAI5>();
                            carAI.m_Car.Move(leader ? -steerDirection : steerDirection, brake, accelerationDirection * acceleration, handBrake);
                        }
                    }
                    else
                    {
                        m_Car.Move(leader ? -steerDirection : steerDirection, brake, accelerationDirection * acceleration, handBrake);
                    }
                }
                else
                {
                    if (formation == Formation.Attack)
                    {
                        foreach (GameObject friend in friends)
                        {
                            CarAI5 carAI = friend.GetComponent<CarAI5>();
                            carAI.m_Car.Move(steerDirection, accelerationDirection * acceleration, -brake, handBrake);
                        }
                    }
                    else
                    {
                        m_Car.Move(steerDirection, accelerationDirection * acceleration, -brake, handBrake);
                    }

                }
            }
            else
            {
                crashTime += Time.deltaTime;
                if (crashTime <= 1f)
                {
                    steerDirection = SteerInput(m_Car.transform.position, m_Car.transform.eulerAngles.y, followPoint);
                    if (crashDirection > 0)
                    {
                        if (formation == Formation.Attack)
                        {
                            foreach (GameObject friend in friends)
                            {
                                CarAI5 carAI = friend.GetComponent<CarAI5>();
                                carAI.m_Car.Move(steerDirection, acceleration, 0, 0);
                            }
                        }
                        else
                        {
                            m_Car.Move(steerDirection, acceleration, 0, 0);
                        }

                    }
                    else
                    {
                        if (formation == Formation.Attack)
                        {
                            foreach (GameObject friend in friends)
                            {
                                CarAI5 carAI = friend.GetComponent<CarAI5>();
                                carAI.m_Car.Move(-steerDirection, 0, -acceleration, 0);
                            }
                        }
                        else
                        {
                            m_Car.Move(-steerDirection, 0, -acceleration, 0);
                        }

                    }
                }
                else
                {
                    crashTime = 0;
                    crashed = false;
                }
            }
        }

        private Vector3 FindFollowPoint()
        {


            if (leader)
            {
                if(inFormation || formation == Formation.Back)
                {
                    return Vector3.zero;
                }
                return finalPath[currentPathIndex];
            }

            Transform leaderCar = transform;
            int leaderCarPathIndex = 0;
            int leaderCarNumber = 0;
            CarAI5 leaderAI = friends[0].GetComponent<CarAI5>();
            foreach (GameObject friend in friends)
            {
                if (friend.GetComponent<CarAI5>().leader)
                {
                    leaderCar = friend.transform;
                    leaderAI = friend.GetComponent<CarAI5>();
                    leaderCarPathIndex = friend.GetComponent<CarAI5>().currentPathIndex;
                    leaderCarNumber = friend.GetComponent<CarAI5>().carNumber;
                    break;
                }
            }

            if(!leaderAI.inFormation)
            {
                return Vector3.zero;
            }
            
            int dir = -1;
            if(carNumber == (leaderCarNumber + 2) % 3)
            {
                dir = 1;
            }

            

            offset = Quaternion.AngleAxis(60 * dir, leaderCar.transform.up) * -leaderCar.transform.forward * 4;

            Debug.Log(carNumber + "   Offset" + offset);
            return leaderCar.position + offset;
        }

        private bool CheckSpacing(Transform leaderCar, float spacing)
        {
            Vector3 pos = leaderCar.position + Quaternion.AngleAxis(-angle, leaderCar.up) * -Vector3.forward * spacing;
            Vector3 pos2 = leaderCar.position + Quaternion.AngleAxis(angle, leaderCar.up) * -Vector3.forward * spacing;
            return terrain_manager.myInfo.traversability[terrain_manager.myInfo.get_i_index(pos.x), terrain_manager.myInfo.get_j_index(pos.z)] > 0.5f
                    || terrain_manager.myInfo.traversability[terrain_manager.myInfo.get_i_index(pos2.x), terrain_manager.myInfo.get_j_index(pos2.z)] > 0.5f;
        }

        //Determines steer angle for the car
        private float SteerInput(Vector3 position, float theta, Vector3 point)
        {
            Vector3 direction = Quaternion.Euler(0, theta, 0) * Vector3.forward;
            Vector3 directionToPoint = point - position;
            float angle = Vector3.Angle(direction, directionToPoint) * Mathf.Sign(-direction.x * directionToPoint.z + direction.z * directionToPoint.x);
            float steerAngle = Mathf.Clamp(angle, -m_Car.m_MaximumSteerAngle, m_Car.m_MaximumSteerAngle) / m_Car.m_MaximumSteerAngle;
            return steerAngle;
        }

        //Determines acceleration for the car
        private float AccelerationInput(Vector3 position, float theta, Vector3 point)
        {
            Vector3 direction = Quaternion.Euler(0, theta, 0) * Vector3.forward;
            Vector3 directionToPoint = point - position;
            return Mathf.Clamp(direction.x * directionToPoint.x + direction.z * directionToPoint.z, -1, 1);
        }

        //Get size of car collider to be used with C space
        private void InitializeCSpace()
        {
            Quaternion carRotation = m_Car.transform.rotation;
            m_Car.transform.rotation = Quaternion.identity;
            configurationSpace = new ConfigurationSpace();
            BoxCollider carCollider = GameObject.Find("ColliderBottom").GetComponent<BoxCollider>();
            configurationSpace.BoxSize = carCollider.transform.TransformVector(carCollider.size);
            m_Car.transform.rotation = carRotation;
        }

        private void OnDrawGizmos()
        {
            if(!Application.isPlaying || leader)
            {
                return;
            }

            Transform leaderCar = transform;
            foreach (GameObject friend in friends)
            {
                if (friend.GetComponent<CarAI5>().leader)
                {
                    leaderCar = friend.transform;
                    break;
                }
            }
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(leaderCar.position, 1);


            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(leaderCar.position + offset, 1);
        }
    }
}
