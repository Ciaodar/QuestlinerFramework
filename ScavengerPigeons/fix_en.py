import re
import os

filepath = 'c:/Users/meteh/RiderProjects/QuestlinerFramework/ScavengerPigeons/ModuleData/Languages/EN/std_ScavengerPigeons_en.xml'
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace exact phrases to be context-appropriate
reps = {
    "Dost's Shadow Assassins": "The friend's Shadow Assassins",
    "Dost's elite blades": "your friend's elite blades",
    "crest of Dost and the": "crest of your friend and the",
    "Dost's puppet empire": "your friend's puppet empire",
    "loyalty, Dost gifted": "loyalty, your friend gifted",
    "But Dost already senses": "But your friend already senses",
    "behalf of Dost.": "behalf of my friend.",
    "ciphers for Dost.": "ciphers for your friend.",
    "cylinder from Dost for": "cylinder from my friend for",
    "directives from Dost.": "directives from my friend.",
    "exactly as Dost demands.": "exactly as your friend demands.",
    "If Dost is trading": "If my friend is trading",
    "while Dost parades": "while your friend parades",
    "while Dost traded": "while my friend traded",
    "Dost's grand design": "my friend's grand design",
    "Dost systematically": "Your friend systematically",
    "Dost's stolen": "your friend's stolen",
    "Let Dost find": "Let him find",
    "We meet again, Dost.": "We meet again, old friend.",
    "You are weak, Dost.": "You are weak, old friend.",
    "We were allies once, Dost.": "We were allies once, old friend.",
    "not a protector, Dost—you": "not a protector, old friend—you",
    "not a protector, Dostyou": "not a protector, old friendyou", # Handle encoding issues just in case
    "metaphors. Dost is dead": "metaphors. My old friend is dead",
    "ends here, Dost.": "ends here, old friend.",
    "trim it, Dost. Or die": "trim it, old friend. Or die",
    "Dost. Stand down.": "Old friend. Stand down.",
    "It is over, Dost.": "It is over, old friend.",
    "shadows, Dost.": "shadows, old friend.",
    "Dost's elite death squad": "the friend's elite death squad",
    "Dost's silencers": "your friend's silencers",
    "paid Dost to assassinate": "paid your friend to assassinate",
    "Dost orchestrates": "your friend orchestrates",
    "Dost's ultimate insurance": "his ultimate insurance",
    "Dost's hounds": "your friend's hounds",
    "laid out by Dost?": "laid out by your friend?",
    "How deeply is Dost involved": "How deeply is my friend involved",
    "If Dost keeps": "If my friend keeps",
    "Let Dost send": "Let him send",
    "Dost is not serving": "Your friend is not serving",
    "Dost and the Scavenger": "your friend and the Scavenger",
    "noble friend Dost.": "noble friend.",
    "raid Dost orchestrated": "raid your friend orchestrated",
    "Dost will never know": "Your friend will never know",
    "Dost warned me": "My friend warned me",
    "Even if Dost is ruthless": "Even if my friend is ruthless",
    "Dost is the real puppeteer": "Your friend is the real puppeteer",
    "seeking Dost's seat": "seeking your friend's seat",
    "over Dost?": "over your friend?",
    "orders Dost signed": "orders your friend signed",
    "oath to Dost and": "oath to my friend and",
    "Dost's false wars": "my friend's false wars",
    "Stand with Dost": "Stand with your friend",
    "against Dost": "against your friend",
    "believe you over Dost": "believe you over my friend"
}

for old, new in reps.items():
    content = content.replace(old, new)

# Just in case, do a regex pass for remaining "Dost's" and "Dost" that aren't variables
# This will catch anything missed by the dictionary
def fallback_replacer(match):
    word = match.group(0)
    if "Dost's" in word:
        return "your friend's"
    return "your friend"

# Negative lookbehinds/lookaheads to exclude {Dost... and Dost_Link
content = re.sub(r'(?<!\{)\bDost\'s\b(?!_Link|\.Name)', "your friend's", content)
content = re.sub(r'(?<!\{)\bDost\b(?!_Link|\.Name)', "your friend", content)


with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print("Done replacements in EN localization.")
