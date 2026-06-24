public class InteractionResolver
{
    public InteractionResolver(){}
    public ResolvedInteraction Resolve(
        InteractionEvent e,
        Interactable target,
        bool isGrabbing
    )
    {
        // GRAB
        if (e.type == GestureType.GRAB)
        {
            return new ResolvedInteraction
            {
                type = InteractionType.Grab,
                target = target,
                source = e
            };
        }

        // PINCH
        if (e.type == GestureType.PINCH && !isGrabbing)
        {
            if (target != null && target.CanInteract(InteractionType.Select))
            {
                return new ResolvedInteraction
                {
                    type = InteractionType.Select,
                    target = target,
                    source = e
                };
            }

            return new ResolvedInteraction
            {
                type = InteractionType.Pinch,
                target = target,
                source = e
            };
        }

        // ROTATE continuo
        if (e.type == GestureType.ROTATE)// && isGrabbing)
        {
            return new ResolvedInteraction
            {
                type = InteractionType.Rotate,
                target = target,
                source = e
            };
        }

        return default;
    }
}