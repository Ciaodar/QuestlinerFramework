import os

replacements = {
    'quest_dostun_gorevleri.xml': [
        ('{=sp_dg_s1_log}Spring Service - Espionage: A pigeon from {Dost_Link}', '{=sp_dg_s1_log}Spring Service - Espionage: A pigeon from {Dost.Name}'),
        ("{=sp_dg_s2_wait_log}The Spring espionage task was completed. As summer sweeps across Calradia, you await {Dost_Link}'s", "{=sp_dg_s2_wait_log}The Spring espionage task was completed. As summer sweeps across Calradia, you await {Dost.Name}'s"),
        ('{=sp_dg_s3_wait_log}The dispatch reached its destination. As autumn leaves fall, {Dost_Link} summons', '{=sp_dg_s3_wait_log}The dispatch reached its destination. As autumn leaves fall, {Dost.Name} summons'),
        ('{=sp_dg_s5_log}The four seasons of service are complete. But an unmarked black raven has arrived, directing you to meet {DerinKanatUlagi_Link}. The inner council has issued a shocking, unprecedented assassination decree: The target is none other than {Dost_Link}!', '{=sp_dg_s5_log}The four seasons of service are complete. But an unmarked black raven has arrived, directing you to meet {DerinKanatUlagi_Link}. The inner council has issued a shocking, unprecedented assassination decree: The target is none other than {Dost.Name}!'),
        ('{=sp_dg_s_korsadakat_log}You refused to entertain the words of a traitor and struck down {DerinKanatUlagi_Link} where he stood. You rode to {Dost_Link}', '{=sp_dg_s_korsadakat_log}You refused to entertain the words of a traitor and struck down {DerinKanatUlagi_Link} where he stood. You rode to {Dost.Name}'),
        ('{=sp_dg_s_golgerotasi_log}You held your blade and listened to {DerinKanatUlagi_Link}. The documents he provided tore the veil from your eyes: {Dost_Link}', '{=sp_dg_s_golgerotasi_log}You held your blade and listened to {DerinKanatUlagi_Link}. The documents he provided tore the veil from your eyes: {Dost.Name}')
    ],
    'quest_golgetunegi_tasfiye.xml': [
        ("{=sp_gt2_s_success_log}You broke {Dost_Link}'s army", "{=sp_gt2_s_success_log}You broke {Dost.Name}'s army")
    ],
    'quest_itibardersi.xml': [
        ("{=sp_id_s_success_log}You found the barber and asked him 'What is the name of the looter?'. He told you about the slanders against you. An old friend from your motherland, {Dost_Link}, asked", "{=sp_id_s_success_log}You found the barber and asked him 'What is the name of the looter?'. He told you about the slanders against you. An old friend from your motherland, {Dost.Name}, asked")
    ],
    'quest_kopukbaglar.xml': [
        ('{=sp_kb_s2_log}You spoke to {Dost_Link}.', '{=sp_kb_s2_log}You spoke to {Dost.Name}.'),
        ('{=sp_kb_success_log}You destroyed the convoy and executed the traitor {HainZanaatkar_Link}. The recovered ciphers were returned to {Dost_Link}.', '{=sp_kb_success_log}You destroyed the convoy and executed the traitor {HainZanaatkar_Link}. The recovered ciphers were returned to {Dost.Name}.')
    ],
    'quest_yadogruysa.xml': [
        ("{=sp_yd_s_success_log}You found the barber and asked him 'What is the name of the looter?'. He told you about the slanders against you. An old friend from your motherland, {Dost_Link}, asked", "{=sp_yd_s_success_log}You found the barber and asked him 'What is the name of the looter?'. He told you about the slanders against you. An old friend from your motherland, {Dost.Name}, asked")
    ]
}

base_path = 'c:/Users/meteh/RiderProjects/QuestlinerFramework/ScavengerPigeons/ModuleData/Questlines/'

for filename, reps in replacements.items():
    filepath = os.path.join(base_path, filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    for old, new in reps:
        content = content.replace(old, new)
        
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f'Updated {filename}')
