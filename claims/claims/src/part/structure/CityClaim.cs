using System.Collections.Generic;

namespace claims.src.part.structure
{
    public class CityClaim
    {
        List<Cuboid> cuboids = new List<Cuboid>();

        public CityClaim()
        {

        }

        public List<Cuboid> getCuboids()
        {
            return cuboids;
        }
    }
}
