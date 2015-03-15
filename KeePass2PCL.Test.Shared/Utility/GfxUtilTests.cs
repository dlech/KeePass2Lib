﻿using NUnit.Framework;
using System;

#if KeePassLib
using KeePassLib.Utility;
#else
using Splat;
using KeePass2PCL.Utility;
#endif

namespace KeePass2PCL.Test.Shared.Utility
{
    [TestFixture ()]
    public class GfxUtilTests
    {
        // 16x16 all white PNG file, base64 encoded
        const string testImageData =
            "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAACXBIWXMAAAsTAAA" +
            "LEwEAmpwYAAAAB3RJTUUH3wMOFgIgmTCUMQAAABl0RVh0Q29tbWVudABDcmVhdG" +
            "VkIHdpdGggR0lNUFeBDhcAAAAaSURBVCjPY/z//z8DKYCJgUQwqmFUw9DRAABVb" +
            "QMdny4VogAAAABJRU5ErkJggg==";

        [Test ()]
        public void TestLoadImage ()
        {
            var testData = Convert.FromBase64String (testImageData);
            var image = GfxUtil.LoadImage (testData);
            Assert.That (image.Width, Is.EqualTo (16));
            Assert.That (image.Height, Is.EqualTo (16));
#if !KeePassLib
            var nativeImage = image.ToNative ();
            Assert.That (nativeImage.Width, Is.EqualTo (16));
            Assert.That (nativeImage.Height, Is.EqualTo (16));
#endif
        }
    }
}
