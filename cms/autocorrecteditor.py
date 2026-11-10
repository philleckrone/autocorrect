import ScriptureObjects
import os
import subprocess

selectedProject = ScriptureObjects.ScriptureText(Project)
projectDir = selectedProject.directory

autocorrectPath = os.path.join(projectDir, "autocorrect.txt")

exePath = os.path.abspath(os.path.join(projectDir, "..", "cms", "AutoCorrectEditorCms.exe"))

if not os.path.exists(exePath):
    print("AutoCorrectEditorCms.exe not found at: " + exePath)
else:
    subprocess.Popen([exePath, autocorrectPath])
