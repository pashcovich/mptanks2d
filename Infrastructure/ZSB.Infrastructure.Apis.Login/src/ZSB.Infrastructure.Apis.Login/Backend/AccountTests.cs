﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZSB.Infrastructure.Apis.Account.Backend
{
    public static class AccountTests
    {
        public class AccountTest
        {
            public string Question { get; set; }
            public string Answer { get; set; }
        }

        public static AccountTest[] TestOptions = {
            new AccountTest
            {
                Question = "On a color wheel, what color is between yellow and red?",
                Answer = "orange"
            },
            new AccountTest
            {
                Question = "What color is in between blue and green (hint: t__l)?",
                Answer = "teal"
            },
            new AccountTest
            {
                Question = "If there are two cats and three more show up, how many cats are there?",
                Answer = "5"
            },
            new AccountTest
            {
                Question = "How many letters are in the english alphabet?",
                Answer = "26"
            },
            new AccountTest
            {
                Question = "Do lights [brighten] or [darken] rooms?",
                Answer = "brighten"
            },
            new AccountTest
            {
                Question = "Does a clock tell time or distance?",
                Answer = "time"
            },
            new AccountTest
            {
                Question = "Is cotton used in clothes (yes/no)?",
                Answer = "yes"
            },
            new AccountTest
            {
                Question = "Are smartphones tablets (yes | no)?",
                Answer = "no"
            },
            new AccountTest
            {
                Question = "Fill this sequence in: Q, R, _, T, U, V?",
                Answer = "s"
            },
            new AccountTest
            {
                Question = "Can pigs fly (yes \\ no)?",
                Answer = "no"
            },
            new AccountTest
            {
                Question = "Can whales fly (yes \\ no)?",
                Answer = "no"
            },
            new AccountTest
            {
                Question = "Are seats for sitting (yes or no)?",
                Answer = "yes"
            }
        };

        public struct QuestionInfo
        {
            public string Question { get; set; }
            public int Offset { get; set; }
        }

        private static Random rand = new Random();
        public static QuestionInfo GetRandomQuestion()
        {
            int offset = -1;
            while (offset < 0 || offset >= TestOptions.Length)
                offset = rand.Next(0, TestOptions.Length);
            return new QuestionInfo
            {
                Question = TestOptions[offset].Question,
                Offset = offset
            };
        }

        public static bool ValidateChallenge(int questionNumber, string answer)
        {
            if (questionNumber < 0 || questionNumber >= TestOptions.Length)
                return false;

            if (TestOptions[questionNumber].Answer == answer.ToLower().Trim())
                return true;
            return false;
        }
    }
}
