using UnityEngine;

public static class OctreeRaycasting {
    public static bool RayIntersectsBox(Ray ray, Vector3 boxStart, Vector3 boxEnd) {
        
        // Ray marching yeah why not
        float distance = 10;
        int steps = 0;
        Vector3 currPoint = ray.origin;
        distance = DistanceToBox(currPoint, boxStart, boxEnd);

        while (steps < 512) {

            currPoint += ray.direction.normalized * distance;
            distance = DistanceToBox(currPoint, boxStart, boxEnd);
            steps++;

            if (distance < 0.25f) {
                return true;
            }
        }
        
        return false;
    }
    public static bool RayIntersectsBox(Ray ray, Vector3 boxStart, Vector3 boxEnd, out float distance) {
        
        // Ray marching yeah why not
        distance = 10;
        int steps = 0;
        Vector3 currPoint = ray.origin;
        distance = DistanceToBox(currPoint, boxStart, boxEnd);

        while (steps < 512) {

            currPoint += ray.direction.normalized * distance;
            distance = DistanceToBox(currPoint, boxStart, boxEnd);
            steps++;

            if (distance < 0.25f) {
                return true;
            }
        }
        
        return false;
    }

    public static float DistanceToBox (Vector3 p, Vector3 boxMin, Vector3 boxMax) {
        float dx = Mathf.Max(boxMin.x - p.x, 0, p.x - boxMax.x);
        float dy = Mathf.Max(boxMin.y - p.y, 0, p.y - boxMax.y);
        float dz = Mathf.Max(boxMin.z - p.z, 0, p.z - boxMax.z);
        return Mathf.Pow(dx*dx + dy*dy + dz*dz, 1f/3f);
    }

    public static float DistanceToALine(Vector3 p, Ray lineRay) {
        
        Vector3 v = lineRay.direction;
        Vector3 PQ = p - lineRay.origin;

        return (Vector3.Cross(PQ, v).magnitude) / v.magnitude; 

    }
 
    public static Vector3 HitNormalOfACube(Ray ray, Vector3 boxStart, Vector3 boxEnd, out Vector3 hitPoint) {

        // Approach until hit
        // Ray marching 
        float distance = 10;
        int steps = 0;
        Vector3 currPoint = ray.origin;
        distance = DistanceToBox(currPoint, boxStart, boxEnd);

        while (steps < 512) {

            currPoint += ray.direction.normalized * distance;
            distance = DistanceToBox(currPoint, boxStart, boxEnd);
            steps++;

            if (distance < 0.25f) {
                hitPoint = currPoint;
                return NormalOfRayToBox(ray, boxStart, boxEnd);
            }
        }

        hitPoint = Vector3.zero;
        return Vector3.zero;
    }
    public static Vector3 HitNormalOfACube(Ray ray, Vector3 boxStart, Vector3 boxEnd) {

        Vector3 hitPoint;
        return HitNormalOfACube(ray, boxStart, boxEnd, out hitPoint);
    }

    // TODO: fix hack
    public static Vector3 NormalOfRayToBox(Ray ray, Vector3 boxMin, Vector3 boxMax) {
        GameObject box = new GameObject();
        box.transform.position = (boxMin + boxMax) / 2;
        box.AddComponent<BoxCollider>().size = boxMax - boxMin;
        
        RaycastHit hit;
        Physics.Raycast(ray, out hit);
        
        GameObject.Destroy(box);
        return hit.normal;
    }

    public static bool BoxContainsPoint(Vector3 boxMin, Vector3 boxMax, Vector3 p) {
        
        return (
            p.x >= boxMin.x && p.x <= boxMax.x &&
            p.y >= boxMin.y && p.y <= boxMax.y &&
            p.z >= boxMin.z && p.z <= boxMax.z 
        );
    }
}
