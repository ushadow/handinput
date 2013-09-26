Readme for STIP-VelocityFeatures
10-29-2009
by Ross Messing
rmessing@cs.rochester.edu

This program extracts velocity history features from video.  It is built on  code by Schuldt, Laptev and Caputo (see citation).  This uses their space time interest point (STIP) detector, but replaces their descriptor with the velocity history descriptor from the 2009 paper by Messing, Pal, and Kautz.  This descriptor is a sequence of scale-sensitive 2D positions, found by the KLT tracker (implementation from OpenCV, though the affine variant had to be hacked a little to make it run) around the detected STIP point (starting from the frame in which the STIP feature was detected, and tracking that point forward in time until it disappears).   Command line options are available from the executable.  You might need to ignore the finest level of spatial detail using the -nplev argument in order to get the extractor working with high resolution movies.  

Please let me know if you decide to use this, just so I can get an idea of who's making use of it.  For non-academic users, please contact us for permission before you use this code.  This condition may chang later, but until we decide on a license, we give conservative conditions.

Lastly, please contact me with any feedback, bugs, feature requests, e.t.c. The worst I'll do is ignore you.  And if you add features, I'd like the opportunity to add them into this distribution, too.

Thanks!
-Ross Messing
10-26-2009

P.S. Compilation help:

I can't help you with compilation.  If you're using Visual C++ for Windows, I really can't help you (haven't tried it), but if you're using the makefile (modified from the original one that came with the STIP detector), I probably can't help you, but I'll try (or at least, try to try).  My best naive advice is to check any paths I may have hardcoded in there.  In particular, if openCV doesn't live in /usr/local/include/opencv, you'll have to change the CPPFLAGS on line 14 of the makefile.

Bibliography

@InProceedings{schuldt:04, AUTHOR     = {Sch{\"u}ldt, C. and Laptev, I. and Caputo, B.}, TITLE      = {Recognizing Human Actions: a Local {SVM} Approach}, booktitle  = "Proc. of ICPR", YEAR       = "2004", pages      = "III:32--36"}

@inproceedings{iccv2009MessingPalKautz, author = {Ross Messing and Chris Pal and Henry Kautz}, title = {Activity recognition using the velocity histories of tracked keypoints}, booktitle = {ICCV '09: Proceedings of the Twelfth IEEE International Conference on Computer Vision}, year = {2009}, publisher = {IEEE Computer Society}, address = {Washington, DC, USA}, }