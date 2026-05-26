using UnityEngine;

public class RotacionPivote : MonoBehaviour
{
    public Vector3 pivot = new(0.5f, -0.5f, 0f);
    public float velocidad = 90f; // grados/segundo
    public float anguloObjetivo = 90f;
    public bool rotarObjeto = true;
    private float anguloAcumm = 0;
    void Update()
    {
        float angulo = velocidad * Time.deltaTime;
        anguloAcumm += angulo;

        // Si nos pasamos del objetivo, ajustamos el último paso
        if (anguloAcumm >= anguloObjetivo)
        {
            angulo -= (anguloAcumm - anguloObjetivo); // Ajuste fino
            anguloAcumm = anguloObjetivo;
        }

        Quaternion rotacion = Quaternion.Euler(angulo, 0, 0);

        Vector3 offset = transform.position - pivot;
        offset = rotacion * offset;
        transform.position = pivot + offset;

        if (rotarObjeto)
            transform.rotation = rotacion * transform.rotation;

        // Si llegamos al objetivo, avanzamos el pivote
        if (anguloAcumm >= anguloObjetivo)
        {
            pivot.z += 1f;
            anguloAcumm = 0;
        }
    }
}