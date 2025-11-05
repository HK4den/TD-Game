Random unorganized instructions for you 

Make a GitHub account



Download:
Unity Hub
Unity Version 6.2 (6001.2.10f1)
GitHub desktop
git lfs (at git-lfs.com)
git bash (at git-scm.com/install/windows)

Use this video for setting up the GitHub file with unity
https://www.youtube.com/watch?v=pn1YgU81GUY

Use this in case you need to do git LFS because you end up needing to push something big (over 100mb) for whatever reason.
https://www.youtube.com/watch?v=4Ln6iRh_LTo

Git LFS stuff:

For simplicity basically this is just another GitHub related thing that allows pushing bigger files, LFS meaning large file storage. You will need git bash and git lfs downloaded. 

git lfs install
git lfs track "*.unity"
git lfs track "*.mp4"
git lfs track "*.dylib"

After adding all of these then the file size limitation shouldn't be an issue, but if theres another file that it gets mad at you for then add its file type to it. Also if you're having to push big stuff that's requiring git LFS
