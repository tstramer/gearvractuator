data = tdfread('data.csv',',');
cameraOffset = .1;
data.dist = data.dist - cameraOffset;
queryPoints = data.dist(1):.1:data.dist(end);
steps = interp1(data.dist, data.steps, queryPoints, 'linear');
plot(queryPoints, steps)
hold on
plot(data.dist, data.steps, 'o')
hold off
fid = fopen('../unity/Assets/Scripts/CalibrationData.cs','w');
fprintf(fid, '// CALIBRATION DATA\n');
fprintf(fid, '// ****************\n\n');
fprintf(fid, 'public class CalibrationData {\n');
fprintf(fid, '  public static float[] distances = new float[]{%sf};\n', regexprep(strrep(mat2str(queryPoints), ' ', 'f,'), '[\[\]]', ''));
fprintf(fid, '  public static float[] steps = new float[]{%sf};\n', regexprep(strrep(mat2str(steps), ' ', 'f,'), '[\[\]]', ''));
fprintf(fid, '}');
fclose(fid);
