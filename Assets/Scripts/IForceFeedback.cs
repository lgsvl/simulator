/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


public interface IForceFeedback
{
    void SetConstantForce(int force);
    void SetDamperForce(int force);
    void InitSpringForce(int sat, int coeff);
    void SetSpringForce(int sat, int coeff);
    void StopSpringForce();
}
