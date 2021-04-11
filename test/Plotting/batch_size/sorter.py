import pandas as pd

cluster = 'timeout'
# df = pd.read_csv(str(cluster)+'/receiver_log_'+str(cluster)+'.txt')
df = pd.read_csv(str(cluster)+'/receiver_log.txt')

points = list(set(df.amount))

for p in points:
    p_df = df[df.amount==p]
    p_df.sort_values(by='val').reset_index(drop=True).to_csv(str(cluster)+'/split/log_'+str(cluster)+'_'+str(p)+'.csv', index=True)

